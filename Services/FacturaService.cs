using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using ApiProveedores.Dto.Proveedor;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;
using ApiProveedores.Models.Factura;
using ApiProveedores.Services.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Google.Cloud.PubSub.V1;
using FacturaEntidad = ApiProveedores.Models.Factura.Factura;
using ApiProveedores.Models.Enum;
using ApiProveedores.Services.PubSub;

namespace ApiProveedores.Services;

/// <summary>
/// Lectura y materialización de CFDI (XML) a modelos tipados para validaciones posteriores.
/// </summary>
public class FacturaService
{
    private static readonly XmlSerializer Serializer = new(typeof(CfdiComprobante));
    private readonly OrdenCompraService _ordenCompraService;
    private readonly ProveedoresService _proveedoresService;
    private readonly PortalDbContext _db;
    private readonly StorageService _storageService;

    public FacturaService(OrdenCompraService ordenCompraService, ProveedoresService proveedoresService, PortalDbContext db, StorageService storageService)
    {
        _ordenCompraService = ordenCompraService;
        _proveedoresService = proveedoresService;
        _db = db;
        _storageService = storageService;
    }

    // Normalize DateTime? to UTC with Kind = Utc. Returns null if input null.
    private static DateTime? NormalizeToUtc(DateTime? dt)
    {
        if (!dt.HasValue) return null;

        var value = dt.Value;
        if (value.Kind == DateTimeKind.Utc) return value;

        // Convert to UTC. If Kind is Unspecified, ToUniversalTime treats it as local;
        // this is acceptable if source times are local or already normalized. Ensure resulting Kind=Utc.
        var utc = value.ToUniversalTime();
        return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
    }

    /// <summary>
    /// Deserializa un CFDI 4.0 desde un stream (posición inicial; no cierra el stream).
    /// </summary>
    public FacturaCfdiDocumento ObtenerFacturaDesdeXml(Stream xmlStream)
    {
        if (xmlStream == null)
            throw new ArgumentNullException(nameof(xmlStream));

        try
        {
            if (xmlStream.CanSeek)
                xmlStream.Position = 0;

            var settings = new XmlReaderSettings
            {
                CloseInput = false,
                IgnoreWhitespace = true,
                DtdProcessing = DtdProcessing.Prohibit
            };

            using var reader = XmlReader.Create(xmlStream, settings);
            var obj = Serializer.Deserialize(reader);
            if (obj is not CfdiComprobante comprobante)
                throw new ApiProveedoresException("El XML no corresponde a un Comprobante CFDI válido.");

            return FacturaCfdiDocumento.From(comprobante);
        }
        catch (ApiProveedoresException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            throw new ApiProveedoresException($"No se pudo leer el XML de la factura: {ex.Message}");
        }
        catch (XmlException ex)
        {
            throw new ApiProveedoresException($"XML inválido: {ex.Message}");
        }
    }

    /// <summary>
    /// Sobrecarga para contenido en memoria (UTF-8).
    /// </summary>
    public FacturaCfdiDocumento ObtenerFacturaDesdeXml(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ApiProveedoresException("El contenido XML está vacío.");

        var bytes = System.Text.Encoding.UTF8.GetBytes(xmlContent);
        using var ms = new MemoryStream(bytes, writable: false);
        return ObtenerFacturaDesdeXml(ms);
    }

    public async Task<ValidacionFacturaResponseDto> ProcesaCargaFactura(string rfcProveedor, string folioOrdenCompra, string folioRecibo, IFormFile[] file, long idEmpresa)
    {
        if (file == null || file.Length == 0)
            return new ValidacionFacturaResponseDto
            {
                Message = "Archivo no proporcionado.",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
            };

        var archivos = file.Where(f => f != null && f.Length > 0).ToList();
        if (archivos.Count == 0)
            return new ValidacionFacturaResponseDto
            {
                Message = "Ningún archivo tiene contenido.",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
            };

        var xmlFile = archivos.FirstOrDefault(EsArchivoXmlFactura);
        if (xmlFile == null)
            return new ValidacionFacturaResponseDto
            {
                Message = "Se requiere un archivo XML de factura (CFDI).",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
            };

        try
        {
            await using var xmlReadStream = xmlFile.OpenReadStream();
            using var xmlMem = new MemoryStream();
            await xmlReadStream.CopyToAsync(xmlMem);
            var xmlBytes = xmlMem.ToArray();

            var facturaCfdi = ObtenerFacturaDesdeXml(new MemoryStream(xmlBytes, writable: false));

            byte[]? pdfBytes = null;
            var pdfFile = archivos.FirstOrDefault(EsArchivoPdf);
            if (pdfFile != null)
            {
                await using var pdfStream = pdfFile.OpenReadStream();
                using var pdfMem = new MemoryStream();
                await pdfStream.CopyToAsync(pdfMem);
                pdfBytes = pdfMem.ToArray();
            }

            var ordenCompraRecepcion = await _ordenCompraService.GetOrdenRecepcionSinFacturaAsync(rfcProveedor, folioOrdenCompra);

            //var recepciones = await _ordenCompraService.ObtenerRecepcionesPorIdOcAsync(folioRecibo);

            if (ordenCompraRecepcion.Recepciones == null || ordenCompraRecepcion.Recepciones.Count == 0)
                return new ValidacionFacturaResponseDto
                {
                    Message = $"No se encontró una recepción con el folio {folioRecibo}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                    Accion = TipoAccionSiguientejEnum.ErrorEnProceso
                };

            var primeraRecepcion = ordenCompraRecepcion.Recepciones.FirstOrDefault();
            var montoRecepcion = primeraRecepcion?.Subtotal ?? 0;
            var totalFactura = facturaCfdi.SubTotal ?? 0;

            // Si la factura excede el monto de la recepción, se valida contra el sobrante permitido del proveedor.
            // Si excede el sobrante, se guarda la factura con estatus "Pendiente Nota" y se solicita nota de crédito.
            if (primeraRecepcion != null && totalFactura > montoRecepcion)
            {
                ProveedorResponseDto? payloadProveedor;
                try
                {
                    // se obtiene al proveedor para hacer la validación de sobrante en caso de que la factura exceda el monto de la recepción
                    var proveedor = await _proveedoresService.ObtenerInfoProveedorPorRfcAsync(rfcProveedor);
                    payloadProveedor = proveedor.Values.OfType<ProveedorResponseDto>().FirstOrDefault();
                }
                catch (ApiProveedoresException ex)
                {
                    return new ValidacionFacturaResponseDto
                    {
                        Message = ex.Message,
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                        Success = false,
                    };
                }

                if (payloadProveedor == null)
                    return new ValidacionFacturaResponseDto
                    {
                        Message = $"No se encontró un proveedor con el RFC {rfcProveedor}.",
                        StatusCode = System.Net.HttpStatusCode.NotFound,
                        Success = false,
                    };

                var diferenciaFacturaVsRecepcion = totalFactura - montoRecepcion;

                if (diferenciaFacturaVsRecepcion > payloadProveedor.Sobrante)
                {
                    return new ValidacionFacturaResponseDto
                    {
                        Message = $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C}, " +
                                  $"lo cual supera el sobrante permitido para este proveedor.",
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                        Success = false,
                    };

                }

                //var (idProveedor, idEmpresa) = await ObtenerIdsProveedorEmpresaAsync(rfcProveedor);

                var motivo =
                    $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C}, lo cual supera el sobrante permitido para este proveedor.";

                var idFactura = await GuardarFacturaPendienteNotaAsync(
                    facturaCfdi,
                    payloadProveedor.IdProveedor,
                    idEmpresa,
                    primeraRecepcion.IdRecepcion,
                    montoRecepcion,
                    xmlBytes,
                    pdfBytes,
                    folioOrdenCompra,
                    folioRecibo,
                    motivo);

                return new ValidacionFacturaResponseDto
                {
                    Message = motivo,
                    StatusCode = System.Net.HttpStatusCode.Accepted,
                    Success = false,
                    ProcesoId = idFactura.ToString(CultureInfo.InvariantCulture),
                    Accion = TipoAccionSiguientejEnum.SolicitarNotaCredito
                };
            }


            var nombresSubidos = new List<string>();
            foreach (var doc in archivos)
            {
                using var stream = doc.OpenReadStream();
                var fileName = $"{Guid.NewGuid()}_{doc.FileName}";
                var uploadedFileName = await _storageService.UploadFilesAsync(stream, fileName);
                nombresSubidos.Add(uploadedFileName);
            }


            return new ValidacionFacturaResponseDto
            {
                Message = "Validación de factura completada.",
                StatusCode = System.Net.HttpStatusCode.OK,
                Success = true,
            };
        }
        catch (ApiProveedoresException ex)
        {
            return new ValidacionFacturaResponseDto
            {
                Message = ex.Message,
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
            };
        }
        catch (Exception ex)
        {
            return new ValidacionFacturaResponseDto
            {
                Message = ex.Message,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Success = false
            };
        }
    }

    /// <summary>
    /// Persiste la factura con estatus "Pendiente Nota". XML y PDF se guardan en base64 hasta que se suban a storage y se reemplacen por URL.
    /// </summary>
    private async Task<long> GuardarFacturaPendienteNotaAsync(
        FacturaCfdiDocumento cfdi,
        long idProveedor,
        long idEmpresa,
        long idRecepcion,
        decimal montoRecepcion,
        byte[] xmlBytes,
        byte[]? pdfBytes,
        string folioOrdenCompra,
        string noRecepcion,
        string? motivo)
    {
        var comp = cfdi.Comprobante;
        var subtotal = cfdi.SubTotal ?? 0;
        var total = cfdi.Total ?? 0;
        var iva = cfdi.TotalImpuestosTrasladados ?? 0;
        var ahora = DateTime.UtcNow;

        var entity = new FacturaEntidad
        {
            IdProveedor = idProveedor,
            IdEmpresa = idEmpresa,
            TipoDeComprobante = comp.TipoDeComprobante,
            EstatusFactura = "Pendiente Nota",
            FolioOrigen = folioOrdenCompra,
            Folio = comp.Folio,
            Serie = comp.Serie,
            Uuid = cfdi.Uuid,
            Motivo = motivo,
            HayEvidencia = pdfBytes is { Length: > 0 },
            FechaAlta = ahora,
            FechaFactura = NormalizeToUtc(cfdi.FechaComprobante),
            Subtotal = subtotal,
            CdTotal = total,
            Total = total,
            MontoDeRecepcion = montoRecepcion,
            CorreoElectronico = null,
            Xml = Convert.ToBase64String(xmlBytes),
            RepresentacionGrafica = pdfBytes is { Length: > 0 } ? Convert.ToBase64String(pdfBytes) : null,
            UnidadNegocio = null,
            NoOrdenCompra = folioOrdenCompra,
            NoRecepcion = noRecepcion,
            VersionCfdi = comp.Version,
            Ieps = 0,
            FechaRegistro = ahora,
            Iva = iva,
            FolioErp = folioOrdenCompra,
            FechaContabilizacion = null,
            FechaCreacion = ahora,
            FechaModificacion = null,
            FacturaRecepcion = new List<FacturaRecepcion>
            {
                new()
                {
                    RecepcionId = idRecepcion
                }
            }
        };

        await _db.Facturas.AddAsync(entity);
        await _db.SaveChangesAsync();

        return entity.IdFactura;
    }

    public async Task<ValidacionFacturaResponseDto> FinalizarFacturaConNotaAsync(IFormFile[] files, long idFactura)
    {
        if (files == null || files.Length == 0)
            return new ValidacionFacturaResponseDto
            {
                Message = "Archivo no proporcionado.",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
            };
        var factura = await _db.Facturas.FindAsync(idFactura);
        if (factura == null)
            return new ValidacionFacturaResponseDto
            {
                Message = $"No se encontró la factura con ID {idFactura}.",
                StatusCode = System.Net.HttpStatusCode.NotFound,
                Success = false,
            };
        try
        {
            var archivos = files.Where(f => f != null && f.Length > 0).ToList();
            var xmlFile = archivos.FirstOrDefault(EsArchivoXmlFactura);

            // Convierte la nota de crédito en un objeto cfdi
            var facturaNotaCreditoCfdi = await ObtenerFacturaCfdi(xmlFile!);

            // Se obtiene informacion de proveedor
            var proveedor = await _proveedoresService.RecuperaProveedorAsync(factura.IdProveedor);

            if(proveedor is null)
            {
                return new ValidacionFacturaResponseDto
                {
                    Message = $"No se encontró el proveedor con ID {factura.IdProveedor}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                };
            }

            // Se obtiene la orden de compra y recepción asociada a la factura para validar que la nota de crédito y factura dan el total correcto
            var numeroRecepcion = factura.NoRecepcion is null ? 0 : long.Parse(factura.NoRecepcion);
            var ordenCompraRecepcion = await _ordenCompraService.GetOrdenIdRecepcionAsync(proveedor.Rfc, factura.FolioOrigen!, numeroRecepcion);

            if(ordenCompraRecepcion is null || ordenCompraRecepcion.Recepciones is null)
            {
                return new ValidacionFacturaResponseDto
                {
                    Message = $"No se encontró la orden de compra y recepción asociada a la factura.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                };
            }

            // Se obtiene la suma de la recepcion y nota de crédito para comparar con el monto de la factura,
            // deben ser iguales o la diferencia debe estar dentro del sobrante permitido para el proveedor
            var totalRecepcionNotaCredito = ordenCompraRecepcion.Recepciones.FirstOrDefault()!.Subtotal + facturaNotaCreditoCfdi.SubTotal;

            var diferenciaRecepcionNotaCreditoVsTotalFactura = factura.Subtotal - totalRecepcionNotaCredito;

            if (diferenciaRecepcionNotaCreditoVsTotalFactura > 0) 
            {
                return new ValidacionFacturaResponseDto
                {
                    Message = $"La suma de la factura y nota de crédito es menor al monto de la recepción por {diferenciaRecepcionNotaCreditoVsTotalFactura:C}.",
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false,
                };
            }

            // No hay diferencia o la diferencia está dentro del sobrante permitido,
            // se guardan los archivo y se actualiza el estatus de la factura a finalizada
            


            byte[]? pdfBytes = null;
            var pdfFile = files.FirstOrDefault(EsArchivoPdf);
            if (pdfFile != null)
            {
                await using var pdfStream = pdfFile.OpenReadStream();
                using var pdfMem = new MemoryStream();
                await pdfStream.CopyToAsync(pdfMem);
                pdfBytes = pdfMem.ToArray();
            }
            factura.EstatusFactura = "Finalizada";
            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                factura.HayEvidencia = true;
                factura.RepresentacionGrafica = Convert.ToBase64String(pdfBytes);
            }
            factura.FechaModificacion = DateTime.UtcNow;
            _db.Facturas.Update(factura);
            await _db.SaveChangesAsync();
            return new ValidacionFacturaResponseDto
            {
                Message = "Factura finalizada correctamente.",
                StatusCode = System.Net.HttpStatusCode.OK,
                Success = true,
            };
        }
        catch (Exception ex)
        {
            return new ValidacionFacturaResponseDto
            {
                Message = ex.Message,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Success = false
            };
        }
    }

    private async Task<(long idProveedor, long idEmpresa)> ObtenerIdsProveedorEmpresaAsync(string rfcProveedor)
    {
        var rfcNorm = rfcProveedor.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
        var prov = await _db.Proveedores
            .Include(p => p.ProveedorEmpresa)
            .FirstOrDefaultAsync(p => p.Rfc != null && p.Rfc.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant() == rfcNorm);

        if (prov == null)
            throw new ApiProveedoresException($"No se encontró un proveedor con el RFC {rfcProveedor}.");

        var idEmpresa = prov.ProveedorEmpresa?.OrderBy(pe => pe.IdRelacionPE).FirstOrDefault()?.IdEmpresa ?? 0;
        if (idEmpresa == 0)
            throw new ApiProveedoresException("El proveedor no tiene empresa asociada.");

        return (prov.Id_proveedor, idEmpresa);
    }

    private static bool EsArchivoPdf(IFormFile doc)
    {
        var ext = Path.GetExtension(doc.FileName);
        if (string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
            return true;
        var ct = doc.ContentType;
        return !string.IsNullOrEmpty(ct) &&
               string.Equals(ct, "application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EsArchivoXmlFactura(IFormFile doc)
    {
        var ext = Path.GetExtension(doc.FileName);
        if (string.Equals(ext, ".xml", StringComparison.OrdinalIgnoreCase))
            return true;
        var ct = doc.ContentType;
        return !string.IsNullOrEmpty(ct) &&
               (ct.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ct, "application/xml", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ct, "text/xml", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<FacturaCfdiDocumento> ObtenerFacturaCfdi(IFormFile doc)
    {
        await using var xmlReadStream = doc.OpenReadStream();
        using var xmlMem = new MemoryStream();
        await xmlReadStream.CopyToAsync(xmlMem);
        var xmlBytes = xmlMem.ToArray();

        var facturaCfdi = ObtenerFacturaDesdeXml(new MemoryStream(xmlBytes, writable: false));
        return facturaCfdi;
    }
}

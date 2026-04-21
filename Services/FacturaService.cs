using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Dto.Proveedor;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;
using ApiProveedores.Models.Enum;
using ApiProveedores.Models.Factura;
using ApiProveedores.Services.Exceptions;
using ApiProveedores.Services.PubSub;
using ClosedXML.Excel;
using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using FacturaEntidad = ApiProveedores.Models.Factura.Factura;

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

    // Método auxiliar para asegurar que DateTime? sea UTC
    private static DateTime? EnsureUtc(DateTime? dt)
    {
        if (!dt.HasValue) return null;
        return dt.Value.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc),
            DateTimeKind.Local => dt.Value.ToUniversalTime(),
            _ => dt
        };
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

    public async Task<ValidacionFacturaResponseDto<bool>> ProcesaCargaFactura(string rfcProveedor, string folioOrdenCompra, string folioRecibo, IFormFile[] file, long idEmpresa)
    {
        if (file == null || file.Length == 0)
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = "Archivo no proporcionado.",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
                Data = false
            };

        var archivos = file.Where(f => f != null && f.Length > 0).ToList();
        if (archivos.Count == 0)
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = "Ningún archivo tiene contenido.",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
                Data = false
            };

        var xmlFile = archivos.FirstOrDefault(EsArchivoXmlFactura);
        if (xmlFile == null)
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = "Se requiere un archivo XML de factura (CFDI).",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
                Data = false
            };

        try
        {
            // se obtiene al proveedor para obtener id y hacer la validación de sobrante en caso de que la factura exceda el monto de la recepción
            var proveedor = await _proveedoresService.ObtenerInfoProveedorPorRfcAsync(rfcProveedor);
            var payloadProveedor = proveedor.Values.OfType<ProveedorResponseDto>().FirstOrDefault();

            if (payloadProveedor == null)
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"No se encontró un proveedor con el RFC {rfcProveedor}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                    Data = false
                };

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

            // Se valida si ya cuenta con factura asignada a la orden de compra o algún error en la consulta a SAP,
            // en ambos casos se regresa error para no continuar con el proceso de validación de la factura
            if (ordenCompraRecepcion is null || !ordenCompraRecepcion.Success)
            {
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = ordenCompraRecepcion is null ? "Error al consultar la orden de compra." : ordenCompraRecepcion.Message,
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false,
                    Data = false
                };
            }

            if (ordenCompraRecepcion.Data!.Recepciones == null || ordenCompraRecepcion.Data!.Recepciones.Count == 0)
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"No se encontró una recepción con el folio {folioRecibo}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                    Data = false,
                    Accion = TipoAccionSiguientejEnum.ErrorEnProceso
                };

            var primeraRecepcion = ordenCompraRecepcion.Data!.Recepciones.FirstOrDefault();
            var montoRecepcion = primeraRecepcion?.Subtotal ?? 0;
            var totalFactura = facturaCfdi.SubTotal ?? 0;

            // Si la factura excede el monto de la recepción, se valida contra el sobrante permitido del proveedor.
            // Si excede el sobrante, se guarda la factura con estatus "Pendiente Nota" y se solicita nota de crédito.
            if (primeraRecepcion != null && totalFactura > montoRecepcion)
            {

                var diferenciaFacturaVsRecepcion = totalFactura - montoRecepcion;

                if (diferenciaFacturaVsRecepcion > payloadProveedor.Sobrante)
                {
                    return new ValidacionFacturaResponseDto<bool>
                    {
                        Message = $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C}, " +
                                  $"lo cual supera el sobrante permitido para este proveedor.",
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                        Success = false,
                        Data = false
                    };

                }

                var motivo =
                    $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C}, por lo cual se solicita nota de crédito.";

                //Se guarda factura con estatus pendiente de nota de crédito
                var idFactura = await GuardarFacturaAsync(
                    facturaCfdi,
                    payloadProveedor.IdProveedor,
                    idEmpresa,
                    primeraRecepcion.IdRecepcion,
                    montoRecepcion,
                    Convert.ToBase64String(xmlBytes),
                    Convert.ToBase64String(pdfBytes),
                    folioOrdenCompra,
                    folioRecibo,
                    motivo,
                    "Pendiente Nota");

                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = motivo,
                    StatusCode = System.Net.HttpStatusCode.Accepted,
                    Success = false,
                    Data = false,
                    ProcesoId = idFactura.ToString(CultureInfo.InvariantCulture),
                    Accion = TipoAccionSiguientejEnum.SolicitarNotaCredito
                };
            }

            //Si la factura es menor al monto de recepción se valida que no exceda el faltante permitido para el proveedor
            if(totalFactura < montoRecepcion)
            {
                var faltante = montoRecepcion - totalFactura;

                if(faltante > payloadProveedor!.Faltante)
                {
                    return new ValidacionFacturaResponseDto<bool>
                    {
                        Message = $"La factura es menor al monto de la recepción por {faltante:C}, " +
                                  $"lo cual supera el faltante permitido para este proveedor.",
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                        Success = false,
                        Data = false
                    };
                }
            }

            // TODO: validar con PAC si el CFDI es correcto y está timbrado

            // Se guardan los archivos en storage y se obtiene la url para guardar en la base de datos
            // primero se guarda en storage
            byte[] xmlFileBytes = ConvertirStreamABytes(xmlReadStream);
            var urlXmlFactura = await _storageService.UploadFilesAsync(new MemoryStream(xmlFileBytes, writable: false),
                $"factura_{facturaCfdi.Uuid}_{Guid.NewGuid()}.xml", "xml");


            //var pdfFileBytes = pdfFile != null ? ConvertirStreamABytes(pdfFile.OpenReadStream()) : null;
            string urlPdf = string.Empty;
            if (pdfFile != null)
            {
                await using var pdfStream = pdfFile.OpenReadStream();
                urlPdf = await _storageService.UploadFilesAsync(pdfStream, $"factura_{facturaCfdi.Uuid}_{Guid.NewGuid()}.pdf", "pdf");
            }

            //Se guarda factura con estatus finalizada
            var idFacturaFinalizada = await GuardarFacturaAsync(
                facturaCfdi,
                payloadProveedor.IdProveedor,
                idEmpresa,
                primeraRecepcion.IdRecepcion,
                montoRecepcion,
                urlXmlFactura,
                urlPdf,
                folioOrdenCompra,
                folioRecibo,
                string.Empty,
                "Finalizada");

            var nombresSubidos = new List<string>();
            foreach (var doc in archivos)
            {
                using var stream = doc.OpenReadStream();
                var fileName = $"{Guid.NewGuid()}_{doc.FileName}";
                var uploadedFileName = await _storageService.UploadFilesAsync(stream, fileName, doc.ContentType);
                nombresSubidos.Add(uploadedFileName);
            }


            return new ValidacionFacturaResponseDto<bool>
            {
                Message = "Validación de factura completada.",
                StatusCode = System.Net.HttpStatusCode.OK,
                Success = true,
                Data = true
            };
        }
        catch (ApiProveedoresException ex)
        {
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = ex.Message,
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
                Data = false
            };
        }
        catch (Exception ex)
        {
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = ex.Message,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Success = false,
                Data = false
            };
        }
    }

    /// <summary>
    /// Guarda la factura en la base de datos con la información proporcionada y la relación con la recepción. Retorna el ID de la factura creada.
    /// </summary>
    private async Task<long> GuardarFacturaAsync(
        FacturaCfdiDocumento cfdi,
        long idProveedor,
        long idEmpresa,
        long idRecepcion,
        decimal montoRecepcion,
        string xmlBytes,
        string? pdfBytes,
        string folioOrdenCompra,
        string noRecepcion,
        string? motivo,
        string? estatus)
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
            EstatusFactura = estatus,
            FolioOrigen = folioOrdenCompra,
            Folio = comp.Folio,
            Serie = comp.Serie,
            Uuid = cfdi.Uuid,
            Motivo = motivo,
            HayEvidencia = pdfBytes is { Length: > 0 },
            FechaAlta = ahora,
            FechaFactura = (DateTime)NormalizeToUtc(cfdi.FechaComprobante),
            Subtotal = subtotal,
            CdTotal = total,
            Total = total,
            MontoDeRecepcion = montoRecepcion,
            CorreoElectronico = null,
            Xml = xmlBytes,
            RepresentacionGrafica = pdfBytes is { Length: > 0 } ? pdfBytes : null,
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

    public async Task<ValidacionFacturaResponseDto<bool>> FinalizarFacturaConNotaAsync(IFormFile[] files, long idFactura, string motivo)
    {
        if (files == null || files.Length == 0)
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = "Archivo no proporcionado.",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
                Data = false
            };
        var factura = await _db.Facturas.FindAsync(idFactura);
        if (factura == null)
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = $"No se encontró la factura con ID {idFactura}.",
                StatusCode = System.Net.HttpStatusCode.NotFound,
                Success = false,
                Data = false
            };
        try
        {
            var archivos = files.Where(f => f != null && f.Length > 0).ToList();
            var xmlNotaCreditoFile = archivos.FirstOrDefault(EsArchivoXmlFactura);

            // Convierte la nota de crédito en un objeto cfdi
            var facturaNotaCreditoCfdi = await ObtenerFacturaCfdi(xmlNotaCreditoFile!);

            // Se obtiene informacion de proveedor
            var proveedor = await _proveedoresService.RecuperaProveedorAsync(factura.IdProveedor);

            if(proveedor is null)
            {
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"No se encontró el proveedor con ID {factura.IdProveedor}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                    Data = false
                };
            }

            // Se obtiene la orden de compra y recepción asociada a la factura para validar que la nota de crédito y factura dan el total correcto
            var numeroRecepcion = factura.NoRecepcion is null ? 0 : long.Parse(factura.NoRecepcion);
            var ordenCompraRecepcion = await _ordenCompraService.GetOrdenIdRecepcionAsync(proveedor.Rfc, factura.FolioOrigen!, numeroRecepcion);

            if(ordenCompraRecepcion is null || ordenCompraRecepcion.Recepciones is null)
            {
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"No se encontró la orden de compra y recepción asociada a la factura.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                    Data = false
                };
            }

            // Se obtiene la suma de la recepcion y nota de crédito para comparar con el monto de la factura,
            // deben ser iguales o la diferencia debe estar dentro del sobrante permitido para el proveedor
            var totalRecepcionNotaCredito = ordenCompraRecepcion.Recepciones.FirstOrDefault()!.Subtotal + facturaNotaCreditoCfdi.SubTotal;

            var diferenciaRecepcionNotaCreditoVsTotalFactura = factura.Subtotal - totalRecepcionNotaCredito;

            if (diferenciaRecepcionNotaCreditoVsTotalFactura > proveedor.Sobrante) 
            {
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"La suma de la factura y nota de crédito es menor al monto de la recepción por {diferenciaRecepcionNotaCreditoVsTotalFactura:C}.",
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false,
                    Data = false
                };
            }

            // No hay diferencia o la diferencia está dentro del sobrante permitido,
            // se guardan los archivo y se actualiza el estatus de la factura a finalizada

            // Se toma el archivo xml y pdf de la bd de la factura y se convierte en stream para subir a storage y obtener url
            byte[] xmlFacturaBytes = Convert.FromBase64String(factura.Xml!);
            using var streamFactura = new MemoryStream(xmlFacturaBytes, writable: false);

            var nombreArchivoXml = $"factura_{factura.IdFactura}_{Guid.NewGuid()}.xml";
            var urlXml = await _storageService.UploadFilesAsync(streamFactura, nombreArchivoXml, "xml");
            factura.Xml = urlXml;
            factura.EstatusFactura = "Finalizada";

            if (factura.RepresentacionGrafica is not null)
            {
                byte[] pdfFacturaBytes = Convert.FromBase64String(factura.RepresentacionGrafica);
                using var streamFacturaPdf = new MemoryStream(pdfFacturaBytes, writable: false);

                var nombreArchivoPdf = $"factura_{factura.IdFactura}_{Guid.NewGuid()}.pdf";
                var urlPdf = await _storageService.UploadFilesAsync(streamFacturaPdf, nombreArchivoPdf, "pdf");
                factura.RepresentacionGrafica = urlPdf;
            }

            // se guarda la actualización de las factura con los archivos en storage y estatus finalizada
            // Convertir todas las fechas a UTC antes de guardar
            factura.FechaAlta = EnsureUtc(factura.FechaAlta);
            factura.FechaFactura = (DateTime)EnsureUtc(factura.FechaFactura);
            factura.FechaRegistro = EnsureUtc(factura.FechaRegistro);
            factura.FechaContabilizacion = EnsureUtc(factura.FechaContabilizacion);
            factura.FechaCreacion = EnsureUtc(factura.FechaCreacion) ?? DateTime.UtcNow;
            factura.FechaModificacion = EnsureUtc(factura.FechaModificacion);
            _db.Facturas.Update(factura);
            await _db.SaveChangesAsync();

            // se procede a guardar la nota de crédito con estatus finalizada y los archivos en storage
            // primero se guarda en storage
            byte[] xmlFileBytes = ConvertirStreamABytes(xmlNotaCreditoFile!.OpenReadStream());
            var urlXmlNotaCredito = await _storageService.UploadFilesAsync(new MemoryStream(xmlFileBytes, writable: false), 
                $"nota_credito_{factura.IdFactura}_{Guid.NewGuid()}.xml", "xml");


            var pdfFile = files.FirstOrDefault(EsArchivoPdf);
            string urlPdfNotaCredito = string.Empty;
            if (pdfFile != null)
            {
                await using var pdfStream = pdfFile.OpenReadStream();
                urlPdfNotaCredito = await _storageService.UploadFilesAsync(pdfStream, $"nota_credito_{factura.IdFactura}_{Guid.NewGuid()}.pdf","pdf");
            }

            // Se guarda en BD la nota de crédito con la relación a la factura y recepción, y estatus finalizada
            var idFacturaNotaCredito = await GuardarFacturaAsync(
                    facturaNotaCreditoCfdi,
                    proveedor.Id_proveedor,
                    factura.IdEmpresa,
                    long.Parse(factura.NoRecepcion!),
                    ordenCompraRecepcion.Recepciones.FirstOrDefault()!.Subtotal ?? 0,
                    urlXmlNotaCredito,
                    urlXmlNotaCredito,
                    ordenCompraRecepcion.Folio!,
                    ordenCompraRecepcion.Recepciones.FirstOrDefault()!.Folio!,
                    "por diferencia de monto",
                    "Finalizada");

            return new ValidacionFacturaResponseDto<bool>
            {
                Message = "Factura finalizada correctamente.",
                StatusCode = System.Net.HttpStatusCode.OK,
                Success = true,
                Data = true
            };
        }
        catch (Exception ex)
        {
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = ex.Message,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Success = false,
                Data = false
            };
        }
    }

    public async Task<ResultadoPaginado<Factura>> ConsultarFacturasAsync(int pagina, int tamanioPagina, DateTime? fechaDesde = null, DateTime? fechaHasta = null, string? estatus = null, long? idProveedor = null)
    {
        if (pagina < 1) pagina = 1;
        if (tamanioPagina < 1) tamanioPagina = 10;

        var query = _db.Facturas        
            .AsQueryable();

        if (fechaDesde.HasValue)
            query = query.Where(f => f.FechaFactura >= fechaDesde.Value.ToUniversalTime());

        if (fechaHasta.HasValue)
            query = query.Where(f => f.FechaFactura <= fechaHasta.Value.ToUniversalTime());

        if (!string.IsNullOrEmpty(estatus))
            query = query.Where(f => f.EstatusFactura == estatus);

        if (idProveedor.HasValue)
            query = query.Where(f => f.IdProveedor == idProveedor.Value);

        query = query.OrderBy(f => f.FechaFactura);

        var totalElementos = await query.CountAsync();
        var totalPaginas = (int)Math.Ceiling(totalElementos / (double)tamanioPagina);

        var elementos = await query
                        .Skip((pagina - 1) * tamanioPagina)
                        .Take(tamanioPagina)
                        .ToListAsync();

        return new ResultadoPaginado<Factura>
        {
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            TotalElementos = totalElementos,
            Elementos = elementos
        };
    }

    public async Task<ResultadoPaginado<FacturaConProveedorDto>> ConsultarFacturasConProveedorPaginadoAsync(
        int pagina, int tamanioPagina, long? idProveedor = null, DateTime? fechaInicio = null, DateTime? fechaFin = null, string ? estatus = null)
    {
        var query = _db.Facturas.Include(f => f.Proveedor).AsQueryable();

        if (!string.IsNullOrEmpty(estatus))
            query = query.Where(f => f.EstatusFactura == estatus);
        if (idProveedor.HasValue)
            query = query.Where(f => f.IdProveedor == idProveedor.Value);
        if (fechaInicio.HasValue)
            query = query.Where(f => f.FechaFactura >= fechaInicio.Value.ToUniversalTime());
        if (fechaFin.HasValue)
            query = query.Where(f => f.FechaFactura <= fechaFin.Value.ToUniversalTime());

        var totalElementos = await query.CountAsync();
        var totalPaginas = (int)Math.Ceiling(totalElementos / (double)tamanioPagina);
        var facturas = await query
            .OrderByDescending(f => f.FechaFactura)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .Select(f => new FacturaConProveedorDto
            {
                IdFactura = f.IdFactura,
                IdProveedor = f.IdProveedor,
                NombreProveedor = f.Proveedor != null ? f.Proveedor.Nombre : null,
                TipoDeComprobante = f.TipoDeComprobante,
                EstatusFactura = f.EstatusFactura,
                FolioOrigen = f.FolioOrigen,
                Folio = f.Folio,
                Serie = f.Serie,
                Uuid = f.Uuid,
                Motivo = f.Motivo,
                HayEvidencia = f.HayEvidencia,
                FechaAlta = f.FechaAlta,
                FechaFactura = f.FechaFactura,
                Subtotal = f.Subtotal,
                CdTotal = f.CdTotal,
                Total = f.Total,
                MontoDeRecepcion = f.MontoDeRecepcion,
                CorreoElectronico = f.CorreoElectronico,
                Xml = f.Xml,
                RepresentacionGrafica = f.RepresentacionGrafica,
                UnidadNegocio = f.UnidadNegocio,
                NoOrdenCompra = f.NoOrdenCompra,
                NoRecepcion = f.NoRecepcion,
                VersionCfdi = f.VersionCfdi,
                Ieps = f.Ieps,
                FechaRegistro = f.FechaRegistro,
                Iva = f.Iva,
                FolioErp = f.FolioErp,
                FechaContabilizacion = f.FechaContabilizacion,
                FechaCreacion = f.FechaCreacion,
                FechaModificacion = f.FechaModificacion
            })
            .ToListAsync();

        return new ResultadoPaginado<FacturaConProveedorDto>
        {
            TotalElementos = totalElementos,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            Elementos = facturas
        };
    }

    public async Task<ApiResponseDto<bool>> CargaMasivaFacturasAsync(IFormFile listadoFacturasExcel, IFormFile archivoZip, string rfcProveedor, string emailProveedor)
    {
        if(listadoFacturasExcel == null || listadoFacturasExcel.Length == 0)
        {
            return new ApiResponseDto<bool>
            {
                Message = "Archivo de listado de facturas no proporcionado.",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false
            };
        }

        if(archivoZip == null || archivoZip.Length == 0)
        {
            return new ApiResponseDto<bool>
            {
                Message = "Archivo ZIP con facturas no proporcionado.",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false
            };
        }

        if(!Path.GetExtension(listadoFacturasExcel.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResponseDto<bool>
            {
                Message = "El archivo de listado de facturas debe ser un archivo Excel (.xlsx).",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false
            };
        }

        if(!Path.GetExtension(archivoZip.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResponseDto<bool>
            {
                Message = "El archivo con facturas debe ser un archivo ZIP (.zip).",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false
            };
        }

        try
        {
            // Se toma el archivo de excel para trabajarlo
            bool plantillaUuid = listadoFacturasExcel.FileName.Contains("UUID", StringComparison.OrdinalIgnoreCase);

            List<FacturaCargaDto> facturasExcel = await LeerExcel(listadoFacturasExcel, plantillaUuid);
            //var facturasExcel = new HashSet<string>();
            if (!facturasExcel.Any()) {
                return new ApiResponseDto<bool>
                {
                    Message = "El archivo Excel no contiene datos de facturas o el formato es incorrecto.",
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false
                };
            }

            // Aquí se procesaría el archivo ZIP, se extraerían los XML y PDF, se validarían contra el listado del Excel, y se guardarían en la base de datos y storage.
            using var zipStream = archivoZip.OpenReadStream();
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            if(archive.Entries.Count > 200)
            {
                return new ApiResponseDto<bool>
                {
                    Message = "El archivo ZIP contiene más de 200 facturas, lo cual excede el límite permitido para la carga masiva.",
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false
                };
            }

            var xmls = archive.Entries
                              .Where(x => x.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                              .ToDictionary(
                                 x => Path.GetFileNameWithoutExtension(x.Name).ToUpper(),
                                  x => x);
            var pdfs = archive.Entries
                              .Where(x => x.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                              .ToDictionary(
                                  x => Path.GetFileNameWithoutExtension(x.Name).ToUpper(),
                                  x => x);

            var procesados = new List<ResultadoCargaFacturaGrupalDto>();
            var noProcesados = new List<ResultadoCargaFacturaGrupalDto>();

            // se obtiene al proveedor para obtener id y hacer la validación de sobrante en caso de que la factura exceda el monto de la recepción
            var proveedor = await _proveedoresService.ObtenerInfoProveedorPorRfcAsync(rfcProveedor);
            var payloadProveedor = proveedor.Values.OfType<ProveedorResponseDto>().FirstOrDefault();

            if (payloadProveedor == null)
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"No se encontró un proveedor con el RFC {rfcProveedor}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                    Data = false
                };

            foreach (var factura in facturasExcel)
            {
                string llaveBusqueda = plantillaUuid ? factura.UuidFactura!.ToUpper() : $"{factura.Serie}_{factura.Folio}".ToUpper();

                var keyEncontrada = xmls.Keys.FirstOrDefault(k => k.Split('_').Length >= 3 && k.Split('_', 3)[2] == llaveBusqueda);

                if (keyEncontrada is null)
                {
                    noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                    {
                        OrdenCompra = factura.OrdenCompra,
                        Recepcion = factura.Recepcion,
                        Identificador = llaveBusqueda,
                        Mensaje = "No se encontró el archivo XML correspondiente en el ZIP."
                    });
                    continue;
                }

                var xmlEntry = xmls[keyEncontrada];

                using(var streamXml = xmlEntry.Open())
                {
                    var facturaCfdi = ObtenerFacturaDesdeXml(streamXml);
                    byte[]? pdfBytes = null;

                    var keyEncontradaPdf = pdfs.Keys.FirstOrDefault(k => k.Split('_').Length >= 3 && k.Split('_', 3)[2] == llaveBusqueda);

                    if (keyEncontradaPdf is not null)
                    {
                        var pdfEntry = pdfs[keyEncontradaPdf];
                        using var streamPdf = pdfEntry.Open();
                        using var msPdf = new MemoryStream();
                        await streamPdf.CopyToAsync(msPdf);
                        pdfBytes = msPdf.ToArray();
                    }

                    // Aquí empieza el proceso de validación
                    var ordenCompraRecepcion = await _ordenCompraService.GetOrdenRecepcionSinFacturaAsync(rfcProveedor, factura.OrdenCompra!);

                    // Se valida si ya cuenta con factura asignada a la orden de compra
                    if(ordenCompraRecepcion is null || !ordenCompraRecepcion.Success)
                    {
                        noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                        {
                            OrdenCompra = factura.OrdenCompra,
                            Recepcion = factura.Recepcion,
                            Identificador = llaveBusqueda,
                            Mensaje = "Ya se cuenta con factura asignada a la orden de compra."
                        });
                        continue;
                    }

                    if(ordenCompraRecepcion.Data!.Recepciones == null || ordenCompraRecepcion.Data!.Recepciones.Count == 0)
                    {
                        noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                        {
                            OrdenCompra = factura.OrdenCompra,
                            Recepcion = factura.Recepcion,
                            Identificador = llaveBusqueda,
                            Mensaje = $"No se encontró recepcion con el folio {factura.Recepcion}."
                        });
                        continue;
                    }

                    var primeraRecepcion = ordenCompraRecepcion.Data!.Recepciones.FirstOrDefault();
                    var montoRecepcion = primeraRecepcion!.Subtotal ?? 0;
                    var totalFactura = facturaCfdi.SubTotal ?? 0;

                    // Si la factura excede el monto de la recepción, se valida el sobrante permitodo del proveedor
                    if(primeraRecepcion is not null && totalFactura > montoRecepcion)
                    {
                        var diferenciaFacturaVsRecepcion = totalFactura - montoRecepcion;

                        if(diferenciaFacturaVsRecepcion > payloadProveedor.Sobrante)
                        {
                            noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                            {
                                OrdenCompra = factura.OrdenCompra,
                                Recepcion = factura.Recepcion,
                                Identificador = llaveBusqueda,
                                Mensaje = $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C}, lo cual supera el sobrante permitido para este proveedor."
                            });
                            continue;
                        }

                        //Si no excede el monto permitido se busca el registro de excel para poder obtener el número de la nota de crédito
                        FacturaCargaDto? notaCredito = null;
                        if (plantillaUuid)
                        {
                            notaCredito = facturasExcel.Where(x => x.OrdenCompra == factura.OrdenCompra).FirstOrDefault();
                        }
                        else
                        {
                            notaCredito = facturasExcel.Where(x => x.OrdenCompra == factura.OrdenCompra && x.Documento == "NC").FirstOrDefault();
                        }

                        if(notaCredito is null)
                        {
                            noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                            {
                                OrdenCompra = factura.OrdenCompra,
                                Recepcion = factura.Recepcion,
                                Identificador = llaveBusqueda,
                                Mensaje = $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C} y no se cuenta con Nota de crédito en el listado de excel."
                            });
                            continue;
                        }

                        // Se busca la nota de crédito en el zip recibido
                        //var numeroUuidNotaCredito = notaCredito.UuidNc ?? 

                    }



                    // Por simplicidad, aquí solo se marca como procesada sin hacer las validaciones ni guardados reales.
                    procesados.Add(new ResultadoCargaFacturaGrupalDto
                    {
                        OrdenCompra = factura.OrdenCompra,
                        Recepcion = factura.Recepcion,
                        Identificador = llaveBusqueda,
                        Mensaje = "Factura procesada exitosamente."
                    });
                }

            }


            return new ApiResponseDto<bool>
                {
                    Message = "Carga masiva de facturas procesada exitosamente.",
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Success = true,
                    Data = true
                };
        }
        catch (Exception ex)
        {
            return new ApiResponseDto<bool>
            {
                Message = $"Error al procesar la carga masiva de facturas: {ex.Message}",
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Success = false,
                Data = false
            };
        }
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

    private byte[] ConvertirStreamABytes(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private async Task<List<FacturaCargaDto>> LeerExcel(IFormFile file, bool plantillaUuid)
    {
        var lista = new List<FacturaCargaDto>();

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;

        using var workbook = new XLWorkbook(ms);
        var worksheet = workbook.Worksheets.FirstOrDefault();

        if(worksheet == null)
            throw new ApiProveedoresException("El archivo Excel no contiene ninguna hoja.");

        var lastRow = worksheet.LastRowUsed().RowNumber();

        if(lastRow <= 1)
        {
            throw new ApiProveedoresException("El archivo Excel no contiene datos.");
        }

        for (int row = 2; row <= lastRow; row++)
        {
            if (plantillaUuid)
            {
                lista.Add(new FacturaCargaDto
                {
                    OrdenCompra = worksheet.Cell(row, 2).GetString().Trim(),
                    Recepcion = worksheet.Cell(row, 3).GetString().Trim(),
                    Tienda = worksheet.Cell(row, 4).GetString().Trim(),
                    UuidFactura = worksheet.Cell(row, 5).GetString().Trim(),
                    UuidNc = worksheet.Cell(row, 6).GetString().Trim()
                });
            }
            else
            {
                lista.Add(new FacturaCargaDto
                {
                    OrdenCompra = worksheet.Cell(row, 1).GetString().Trim(),
                    Recepcion = worksheet.Cell(row, 2).GetString().Trim(),
                    Tienda = worksheet.Cell(row, 3).GetString().Trim(),
                    Serie = worksheet.Cell(row, 4).GetString().Trim(),
                    Folio = worksheet.Cell(row, 5).GetString().Trim(),
                    MontoRecepcion = worksheet.Cell(row, 6).GetString().Trim(),
                    Documento = worksheet.Cell(row, 7).GetString().Trim()
                });
            }
        }
        return lista;
    }

}

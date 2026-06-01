using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Dto.Proveedor;
using ApiProveedores.Dto.PubSub;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Helper;
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
using System.Xml.Linq;
using FacturaEntidad = ApiProveedores.Models.Factura.Factura;
using ApiProveedores.Models.ComplementoPago;
using System.Text.Json;
using ApiProveedores.Interfaces;
using Marti.Pac.Abstractions.Interfaces;
using Marti.Pac.Abstractions.Enums;

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
    private readonly ILogger<FacturaService> _logger;
    private readonly GenerarCuerpoEmailHelper _emailHelper;
    private readonly PublisherPnjService _pubSubService;
    private readonly UsuariosService _usuariosService;
    private readonly NotificacionesService _notificacionesService;
    private readonly IPacFactory _pacFactory;

    public FacturaService(OrdenCompraService ordenCompraService, ProveedoresService proveedoresService, PortalDbContext db, StorageService storageService, ILogger<FacturaService> logger, GenerarCuerpoEmailHelper emailHelper, PublisherPnjService pubSubService, UsuariosService usuariosService, NotificacionesService notificacionesService, IPacFactory pacFactory)
    {
        _ordenCompraService = ordenCompraService;
        _proveedoresService = proveedoresService;
        _db = db;
        _storageService = storageService;
        _logger = logger;
        _emailHelper = emailHelper;
        _pubSubService = pubSubService;
        _usuariosService = usuariosService;
        _notificacionesService = notificacionesService;
        _pacFactory = pacFactory;
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
            _logger.LogWarning("No se puedo leer el xml de la factura");
            throw new ApiProveedoresException($"No se pudo leer el XML de la factura: {ex.Message}");
        }
        catch (XmlException ex)
        {
            _logger.LogWarning("XML inválido: {1}", ex.Message);
            throw new ApiProveedoresException($"XML inválido: {ex.Message}");
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex.Message);
            throw new Exception(ex.Message, ex.InnerException);
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
        _logger.LogInformation("Iniciando proceso de carga de factura para proveedor {RfcProveedor}, orden de compra {FolioOrdenCompra}, recepción {FolioRecibo}.", rfcProveedor, folioOrdenCompra, folioRecibo);
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

        var xmlFile = archivos
        .Where(x => Path.GetExtension(x.FileName)
        .Equals(".xml", StringComparison.OrdinalIgnoreCase))
        .ToList().FirstOrDefault();

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
            {
                _logger.LogError("No se encontró un proveedor con el RFC {RfcProveedor}.", rfcProveedor);
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"No se encontró un proveedor con el RFC {rfcProveedor}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                    Data = false
                };

            }

            await using var xmlReadStream = xmlFile.OpenReadStream();
            using var xmlMem = new MemoryStream();
            await xmlReadStream.CopyToAsync(xmlMem);
            var xmlBytes = xmlMem.ToArray();

            var facturaCfdi = ObtenerFacturaDesdeXml(new MemoryStream(xmlBytes, writable: false));

            // Validación PAC
            var pacService = _pacFactory.Create(PacType.Sw);
            var validacionXmlPac = await pacService.ValidaDocumento(xmlFile, xmlBytes);


            if (validacionXmlPac is null && !validacionXmlPac!.IsSuccess && validacionXmlPac.StatusSat != "No encontrado")
            {
                return new ValidacionFacturaResponseDto<bool>
                {
                    Success = false,
                    Accion = TipoAccionSiguientejEnum.ErrorEnProceso,
                    Message = $"No fue exitosa la validación de la PAC, número de UUID: {validacionXmlPac.Uuid}"
                };
            }

            if(validacionXmlPac.StatusSat != "Vigente")
            {
                return new ValidacionFacturaResponseDto<bool>
                {
                    Success = false,
                    Accion = TipoAccionSiguientejEnum.ErrorEnProceso,
                    Message = $"La factura no es vigente según la validación de la PAC, número de UUID: {validacionXmlPac.Uuid}, estatus SAT: {validacionXmlPac.StatusSat}"
                };
            }

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
                _logger.LogError("Error al consultar la orden de compra {FolioOrdenCompra} para el proveedor {RfcProveedor}: {Message}", folioOrdenCompra, rfcProveedor, ordenCompraRecepcion?.Message);
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = ordenCompraRecepcion is null ? "Error al consultar la orden de compra." : ordenCompraRecepcion.Message,
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false,
                    Data = false
                };
            }

            if (ordenCompraRecepcion.Data!.Recepciones == null || ordenCompraRecepcion.Data!.Recepciones.Count == 0)
            {
                _logger.LogError("No se encontró una recepción con el folio {FolioRecibo} para la orden de compra {FolioOrdenCompra} y proveedor {RfcProveedor}.", folioRecibo, folioOrdenCompra, rfcProveedor);
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"No se encontró una recepción con el folio {folioRecibo}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                    Data = false,
                    Accion = TipoAccionSiguientejEnum.ErrorEnProceso
                };
            }

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
                    _logger.LogError("La factura excede el monto de la recepción por {Diferencia:C}, " +
                                      "lo cual supera el sobrante permitido para este proveedor. No. factura: {FolioFactura}", diferenciaFacturaVsRecepcion, facturaCfdi.Uuid);
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
                    EstatusFacturaEnum.PendienteNota);
                _logger.LogInformation("Se solicita nota de crédito para la factura con ID {IdFactura} debido a que excede el monto de la recepción por {Diferencia:C}. No. factura: {FolioFactura}", idFactura, diferenciaFacturaVsRecepcion, facturaCfdi.Uuid);

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
                    _logger.LogError("La factura es menor al monto de la recepción por {Faltante:C}, " +
                                      "lo cual supera el faltante permitido para este proveedor. No. factura: {FolioFactura}", faltante, facturaCfdi.Uuid);
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

           

            // Se guardan los archivos en storage y se obtiene la url para guardar en la base de datos
            // primero se guarda en storage
            byte[] xmlFileBytes = ConvertirStreamABytes(xmlReadStream);
            var urlXmlFactura = await _storageService.UploadFilesAsync(new MemoryStream(xmlFileBytes, writable: false),
                $"factura_{facturaCfdi.Uuid}_{Guid.NewGuid()}.xml", "xml");

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
                EstatusFacturaEnum.Procesada);

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
            _logger.LogError("Error en el proceso de validación de la factura: {Message}", ex.Message);
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
            _logger.LogError("Error inesperado en el proceso de validación de la factura: {Message}", ex.Message);
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
        EstatusFacturaEnum? estatus)
    {
        var comp = cfdi.Comprobante;
        var subtotal = cfdi.SubTotal ?? 0;
        var total = cfdi.Total ?? 0;
        var iva = cfdi.TotalImpuestosTrasladados ?? 0;
        var ahora = DateTime.UtcNow;

        try
        {
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
                FechaFactura = (DateTime)EnsureUtc(cfdi.FechaComprobante),
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
        catch (Exception ex)
        {
            _logger.LogError("Error al guardar la factura en la base de datos para el proveedor con ID {IdProveedor} y orden de compra {FolioOrdenCompra}. Error: {Message}", idProveedor, folioOrdenCompra, ex.Message);
            throw;
        }
        
    }

    public async Task<ValidacionFacturaResponseDto<bool>> FinalizarFacturaConNotaAsync(IFormFile[] files, long idFactura, string motivo)
    {
        _logger.LogInformation("Iniciando proceso de finalización de factura con ID {IdFactura} por motivo: {Motivo}.", idFactura, motivo);
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
        if(factura.EstatusFactura != EstatusFacturaEnum.PendienteNota)
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = $"La factura con ID {idFactura} no tiene el estatus correcto para finalizar con nota de crédito. Estatus: {factura.EstatusFactura}",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
                Data = false
            };
        try
        {
            var archivos = files.Where(f => f != null && f.Length > 0).ToList();
            var xmlNotaCreditoFile = archivos
              .Where(x => Path.GetExtension(x.FileName)
              .Equals(".xml", StringComparison.OrdinalIgnoreCase))
              .ToList().FirstOrDefault();

            if (xmlNotaCreditoFile == null)
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = "Se requiere un archivo XML de nota de crédito (CFDI).",
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false,
                    Data = false
                };

            byte[] xmlFileBytes = ConvertirStreamABytes(xmlNotaCreditoFile!.OpenReadStream());

            //Se realiza la validación con la PAC
            var pacService = _pacFactory.Create(PacType.Sw);
            var respuestaPac = await pacService.ValidaDocumento(xmlNotaCreditoFile, xmlFileBytes);


            if (respuestaPac is null && !respuestaPac!.IsSuccess && respuestaPac.StatusSat != "No encontrado")
            {
                return new ValidacionFacturaResponseDto<bool>
                {
                    Success = false,
                    Accion = TipoAccionSiguientejEnum.ErrorEnProceso,
                    Message = $"No fue exitosa la validación de la PAC, número de UUID: {respuestaPac.Uuid}"
                };
            }

            if (respuestaPac.StatusSat != "Vigente")
            {
                return new ValidacionFacturaResponseDto<bool>
                {
                    Success = false,
                    Accion = TipoAccionSiguientejEnum.ErrorEnProceso,
                    Message = $"La factura no es vigente según la validación de la PAC, número de UUID: {respuestaPac.Uuid}, estatus SAT: {respuestaPac.StatusSat}"
                };
            }

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
                _logger.LogError("No se encontró la orden de compra y recepción asociada a la factura con ID {IdFactura}.", idFactura);
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
            factura.EstatusFactura = EstatusFacturaEnum.Procesada;

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
                    EstatusFacturaEnum.Procesada);

            _logger.LogInformation("Factura con ID {IdFactura} finalizada correctamente con la nota de crédito con ID {IdFacturaNotaCredito}.", idFactura, idFacturaNotaCredito);
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = "Factura finalizada correctamente.",
                StatusCode = System.Net.HttpStatusCode.OK,
                Success = true,
                Data = true,
                Accion = TipoAccionSiguientejEnum.ProcesoCompleto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado en el proceso de finalización de la factura con ID {IdFactura}: {Message}", idFactura, ex.Message);
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = ex.Message,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Success = false,
                Data = false
            };
        }
    }

    public async Task<ResultadoPaginado<Factura>> ConsultarFacturasAsync(int pagina, int tamanioPagina, DateTime? fechaDesde = null, DateTime? fechaHasta = null, EstatusFacturaEnum? estatus = null, long? idProveedor = null)
    {
        if (pagina < 1) pagina = 1;
        if (tamanioPagina < 1) tamanioPagina = 10;

        var query = _db.Facturas        
            .AsQueryable();

        if (fechaDesde.HasValue)
            query = query.Where(f => f.FechaAlta >= fechaDesde.Value.ToUniversalTime());

        if (fechaHasta.HasValue)
            query = query.Where(f => f.FechaAlta <= fechaHasta.Value.ToUniversalTime());

        if (estatus.HasValue)
            query = query.Where(f => f.EstatusFactura == estatus);

        if (idProveedor.HasValue)
            query = query.Where(f => f.IdProveedor == idProveedor.Value);

        query = query.OrderBy(f => f.FechaFactura);

        var totalElementos = await query.CountAsync();
        if(totalElementos == 0)
        {
            return new ResultadoPaginado<Factura>
            {
                PaginaActual = 0,
                TotalPaginas = 0,
                TotalElementos = totalElementos,
                Elementos = new List<FacturaEntidad>()
            };
        }
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

    public async Task<ApiResponseDto<List<Factura>>> ConsultarFacturasCompletoAsync( string rfc, bool porFactura, bool porNota, bool porPago, DateTime? fechaDesde = null, DateTime? fechaHasta = null)
    {
        var query = _db.Facturas
            .AsQueryable();

        if (fechaDesde.HasValue)
            query = query.Where(f => f.FechaFactura >= fechaDesde.Value.ToUniversalTime());

        if (fechaHasta.HasValue)
            query = query.Where(f => f.FechaFactura <= fechaHasta.Value.ToUniversalTime());

        query = query.Where(f => f.RfcProveedor == rfc);

        if (porFactura)
            query = query.Where(f => f.TipoDeComprobante == "I");
        if (porNota)
            query = query.Where(f => f.TipoDeComprobante == "E");
        if (porPago)
            query = query.Where(f => f.TipoDeComprobante == "P");

        query = query.OrderBy(f => f.FechaFactura);

        var elementos = await query.ToListAsync();

        return new ApiResponseDto<List<Factura>>
        {
            Success = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Message = "Consulta realizada correctamente.",
            Data = elementos
        };
    }

    public async Task<ResultadoPaginado<FacturaConProveedorDto>> ConsultarFacturasConProveedorPaginadoAsync(
        int pagina, int tamanioPagina, long? idProveedor = null, DateTime? fechaInicio = null, DateTime? fechaFin = null, EstatusFacturaEnum? estatus = null)
    {
        var query = _db.Facturas.Include(f => f.Proveedor).AsQueryable();

        if (estatus.HasValue)
            query = query.Where(f => f.EstatusFactura == estatus);
        if (idProveedor.HasValue)
            query = query.Where(f => f.IdProveedor == idProveedor.Value);
        if (fechaInicio.HasValue)
            query = query.Where(f => f.FechaFactura >= fechaInicio.Value.ToUniversalTime());
        if (fechaFin.HasValue)
            query = query.Where(f => f.FechaFactura <= fechaFin.Value.ToUniversalTime());

        var totalElementos = await query.CountAsync();
        var totalPaginas = (int)Math.Ceiling(totalElementos / (double)tamanioPagina);
        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError("Error al consultar facturas con proveedor: {Message}", ex.Message);
            throw;
        }
        
    }

    public async Task<ApiResponseDto<CargaMasivaResponse>> CargaMasivaFacturasAsync(IFormFile listadoFacturasExcel, IFormFile archivoZip, string rfcProveedor, string emailProveedor, long usuarioId)
    {
        var resultadoValidaArchivos = ValidarArchivosCargaMasiva(listadoFacturasExcel, archivoZip, rfcProveedor);

        if (!resultadoValidaArchivos.Success)
        {
            return new ApiResponseDto<CargaMasivaResponse>
            {
                Message = resultadoValidaArchivos.Message,
                StatusCode = resultadoValidaArchivos.StatusCode,
                Success = false
            };
        }

        try
        {
            // Se toma el archivo de excel para trabajarlo
            bool plantillaUuid = listadoFacturasExcel.FileName.Contains("UUID", StringComparison.OrdinalIgnoreCase);

            List<FacturaCargaDto> facturasExcel = await LeerExcel(listadoFacturasExcel, plantillaUuid);
            
            if (!facturasExcel.Any()) {
                return new ApiResponseDto<CargaMasivaResponse>
                {
                    Message = "El archivo Excel no contiene datos de facturas o el formato es incorrecto.",
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false
                };
            }

            // Aquí se procesa el archivo ZIP, se extraen los XML y PDF, se validan contra el listado del Excel, y se guardan en la base de datos y storage.
            using var zipStream = archivoZip.OpenReadStream();
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            if(archive.Entries.Count > 200)
            {
                return new ApiResponseDto<CargaMasivaResponse>
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

            if(xmls.Count == 0)
            {
                return new ApiResponseDto<CargaMasivaResponse>
                {
                    Message = "El archivo ZIP no contiene ningún archivo XML de factura.",
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false
                };
            }

            var procesados = new List<ResultadoCargaFacturaGrupalDto>();
            var noProcesados = new List<ResultadoCargaFacturaGrupalDto>();

            // se obtiene al proveedor para obtener id y hacer la validación de sobrante en caso de que la factura exceda el monto de la recepción
            var proveedor = await _proveedoresService.ObtenerInfoProveedorPorRfcAsync(rfcProveedor);
            var payloadProveedor = proveedor.Values.OfType<ProveedorResponseDto>().FirstOrDefault();

            if (payloadProveedor == null)
                return new ApiResponseDto<CargaMasivaResponse>
                {
                    Message = $"No se encontró un proveedor con el RFC {rfcProveedor}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false
                };

            foreach (var facturaExcel in facturasExcel)
            {
                // Si la plantilla es por UUID, se busca el XML con el UUID indicado, si no es por UUID se busca por serie y folio,
                // pero solo si el documento es una factura, si es una nota de crédito no se busca el XML porque no es obligatorio que venga en el ZIP
                if (!plantillaUuid && facturaExcel.Documento == "NC")
                    continue;

                string llaveBusqueda = plantillaUuid ? facturaExcel.UuidFactura!.ToUpper() : $"{facturaExcel.Serie}_{facturaExcel.Folio}".ToUpper();

                var keyEncontrada = xmls.Keys.FirstOrDefault(k => k.Split('_').Length >= 3 && k.Split('_', 3)[2] == llaveBusqueda);

                if (keyEncontrada is null)
                {
                    noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                    {
                        OrdenCompra = facturaExcel.OrdenCompra,
                        Recepcion = facturaExcel.Recepcion,
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

                    // TODO: validar con PAC si el CFDI es correcto y está timbrado
                    using var msXml = new MemoryStream();
                    await streamXml.CopyToAsync(msXml);
                    IFormFile xmlFormFile = new FormFile(msXml, 0, msXml.Length, "file", xmlEntry.Name) { Headers = new HeaderDictionary(), ContentType = "application/xml" };
                    var xmlBytes = msXml.ToArray();
                    var pacService = _pacFactory.Create(PacType.Sw);
                    var validacionXmlPac = await pacService.ValidaDocumento(xmlFormFile, xmlBytes);

                    if (validacionXmlPac is null && !validacionXmlPac!.IsSuccess && validacionXmlPac.StatusSat != "No encontrado")
                    {
                        noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                        {
                            OrdenCompra = facturaExcel.OrdenCompra,
                            Recepcion = facturaExcel.Recepcion,
                            Identificador = llaveBusqueda,
                            Mensaje = $"No se proceso la factura, mensaje de Sat: {validacionXmlPac.StatusSat}"
                        });
                        continue;
                    }

                    if (validacionXmlPac.StatusSat != "Vigente")
                    {
                        noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                        {
                            OrdenCompra = facturaExcel.OrdenCompra,
                            Recepcion = facturaExcel.Recepcion,
                            Identificador = llaveBusqueda,
                            Mensaje = $"No se proceso la factura, mensaje de Sat: {validacionXmlPac.StatusSat}"
                        });
                    }

                    var keyEncontradaPdf = pdfs.Keys.FirstOrDefault(k => k.Split('_').Length >= 3 && k.Split('_', 3)[2] == llaveBusqueda);

                    Stream? pdfIndividual = null;
                    if (keyEncontradaPdf is not null)
                    {
                        var pdfEntry = pdfs[keyEncontradaPdf];
                        using var streamPdf = pdfEntry.Open();
                        using var msPdf = new MemoryStream();
                        await streamPdf.CopyToAsync(msPdf);
                        pdfBytes = msPdf.ToArray();
                        pdfIndividual = new MemoryStream(pdfBytes, writable: false);
                    }

                    // Aquí empieza el proceso de validación
                    var ordenCompraRecepcion = await _ordenCompraService.GetOrdenRecepcionSinFacturaAsync(rfcProveedor, facturaExcel.OrdenCompra!);

                    // Se valida si ya cuenta con factura asignada a la orden de compra
                    if(ordenCompraRecepcion is null || !ordenCompraRecepcion.Success)
                    {
                        noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                        {
                            OrdenCompra = facturaExcel.OrdenCompra,
                            Recepcion = facturaExcel.Recepcion,
                            Identificador = llaveBusqueda,
                            Factura = facturaCfdi.Uuid,
                            Mensaje = $"Ocurrió algún error al consultar la orden de compra. {ordenCompraRecepcion.Message}"
                        });
                        continue;
                    }

                    if(ordenCompraRecepcion.Data!.Recepciones == null || ordenCompraRecepcion.Data!.Recepciones.Count == 0)
                    {
                        noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                        {
                            OrdenCompra = facturaExcel.OrdenCompra,
                            Recepcion = facturaExcel.Recepcion,
                            Identificador = llaveBusqueda,
                            Factura = facturaCfdi.Uuid,
                            Mensaje = $"No se encontró recepcion con el folio {facturaExcel.Recepcion}."
                        });
                        continue;
                    }

                    var primeraRecepcion = ordenCompraRecepcion.Data!.Recepciones.FirstOrDefault();
                    var montoRecepcion = primeraRecepcion!.Subtotal ?? 0;
                    var totalFactura = facturaCfdi.SubTotal ?? 0;

                    // Si la factura excede el monto de la recepción, se valida el sobrante permitodo del proveedor
                    if (primeraRecepcion is not null && totalFactura > montoRecepcion)
                    {
                        var diferenciaFacturaVsRecepcion = totalFactura - montoRecepcion;

                        if (diferenciaFacturaVsRecepcion > payloadProveedor.Sobrante)
                        {
                            noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                            {
                                OrdenCompra = facturaExcel.OrdenCompra,
                                Recepcion = facturaExcel.Recepcion,
                                Identificador = llaveBusqueda,
                                Factura = facturaCfdi.Uuid,
                                Mensaje = $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C}, lo cual supera el sobrante permitido para este proveedor."
                            });
                            continue;
                        }

                        //Se guarda la factura con estatus pendiente de nota de crédito y se procede a buscar la nota de crédito en el listado de excel para validar que venga en el ZIP y
                        //obtener el número de la nota de crédito para relacionarla en la base de datos, y así completar la factura con la nota de crédito y finalizarla
                        //Se guarda factura con estatus pendiente de nota de crédito
                        var xmlNotaCreditoBytes = ConvertirStreamABytes(streamXml); // Aquí se podría cambiar para que en lugar de guardar el XML de la factura, se guarde un XML con la información de la nota de crédito, pero por simplicidad se guarda el mismo XML de la factura
                        var idFactura = await GuardarFacturaAsync(
                            facturaCfdi,
                            payloadProveedor.IdProveedor,
                            0,
                            primeraRecepcion.IdRecepcion,
                            montoRecepcion,
                            Convert.ToBase64String(xmlNotaCreditoBytes),
                            pdfBytes is null ? null : Convert.ToBase64String(pdfBytes),
                            facturaExcel.OrdenCompra!,
                            facturaExcel.Folio!,
                            "No coinciden los montos",
                            EstatusFacturaEnum.PendienteNota);
                        _logger.LogInformation("Se solicita nota de crédito para la factura con ID {IdFactura} debido a que excede el monto de la recepción por {Diferencia:C}. No. factura: {FolioFactura}", idFactura, diferenciaFacturaVsRecepcion, facturaCfdi.Uuid);


                        //Si no excede el monto permitido se busca el registro de excel para poder obtener el número de la nota de crédito
                        FacturaCargaDto? notaCredito = null;
                        if (plantillaUuid)
                        {
                            notaCredito = facturasExcel.Where(x => x.OrdenCompra == facturaExcel.OrdenCompra).FirstOrDefault();
                        }
                        else
                        {
                            notaCredito = facturasExcel.Where(x => x.OrdenCompra == facturaExcel.OrdenCompra && x.Documento == "NC" && x.Serie == facturaExcel.Serie).FirstOrDefault();
                        }

                        if (notaCredito is null)
                        {
                            noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                            {
                                OrdenCompra = facturaExcel.OrdenCompra,
                                Recepcion = facturaExcel.Recepcion,
                                Identificador = llaveBusqueda,
                                Factura = facturaCfdi.Uuid,
                                Mensaje = $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C} y no se cuenta con Nota de crédito en el listado de excel."
                            });
                            continue;
                        }

                        // Se busca la nota de crédito en el zip recibido
                        string? llaveBusquedaNotaCredito = null;

                        if (plantillaUuid)
                        {
                            llaveBusquedaNotaCredito = facturaExcel.UuidNc!.ToUpper();
                        }
                        else
                        {
                            llaveBusquedaNotaCredito = facturasExcel.Where(x => x.OrdenCompra == facturaExcel.OrdenCompra && x.Documento == "NC" && x.Serie == facturaExcel.Serie)
                                                              .Select(x => $"{x.Serie}_{x.Folio}".ToUpper())
                                                              .FirstOrDefault();
                        }

                        if (llaveBusquedaNotaCredito is null)
                        {
                            noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                            {
                                OrdenCompra = facturaExcel.OrdenCompra,
                                Recepcion = facturaExcel.Recepcion,
                                Identificador = llaveBusqueda,
                                Factura = facturaCfdi.Uuid,
                                Mensaje = $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C} y no se cuenta con Nota de crédito en el listado de excel."
                            });
                            continue;
                        }

                        var keyEncontradaNotaCredito = xmls.Keys.FirstOrDefault(k => k.Split('_').Length >= 3 && k.Split('_', 3)[2] == llaveBusquedaNotaCredito);
                        if (keyEncontradaNotaCredito is null)
                        {
                            noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                            {
                                OrdenCompra = facturaExcel.OrdenCompra,
                                Recepcion = facturaExcel.Recepcion,
                                Identificador = llaveBusqueda,
                                Factura = facturaCfdi.Uuid,
                                Mensaje = $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C} y no se encontró el archivo XML de la nota de crédito en el ZIP."
                            });
                            continue;
                        }
                        var xmlEntryNotaCredito = xmls[keyEncontradaNotaCredito];
                        var streamXmlNotaCredito = xmlEntryNotaCredito.Open();
                        streamXmlNotaCredito.Position = 0;
                        var xmlNotaCreditoFormFile = ConvertStreamToIFormFile(streamXmlNotaCredito, xmlEntryNotaCredito.Name);
                        IFormFile[] archivosNotaCredito = new IFormFile[] { xmlNotaCreditoFormFile };

                        //Se busca si cuenta con pdf de la nota de crédito
                        var keyEncontradaNotaCreditoPdf = pdfs.Keys.FirstOrDefault(k => k.Split('_').Length >= 3 && k.Split('_', 3)[2] == llaveBusquedaNotaCredito);

                        Stream? pdfIndividualNotaCredito = null;
                        if (keyEncontradaNotaCreditoPdf is not null)
                        {
                            var pdfEntry = pdfs[keyEncontradaNotaCreditoPdf];
                            if (pdfEntry is not null)
                            {
                                using var streamPdf = pdfEntry.Open();
                                pdfIndividualNotaCredito = streamPdf;
                                var pdfFormFile = ConvertStreamToIFormFile(streamPdf, pdfEntry.Name);
                                archivosNotaCredito = archivosNotaCredito.Append(pdfFormFile).ToArray();
                            }
                        }

                        //Se manda a guardar la nota de crédito y se actualiza la factura estatus finalizada
                        var respuesta = await FinalizarFacturaConNotaAsync(archivosNotaCredito, idFactura, "Diferencia de monto en factura");

                        if (respuesta.Success)
                        {
                            _logger.LogInformation("Factura con ID {IdFactura} finalizada correctamente con la nota de crédito relacionada a la orden de compra {OrdenCompra}.", idFactura, facturaExcel.OrdenCompra);
                            procesados.Add(new ResultadoCargaFacturaGrupalDto
                            {
                                OrdenCompra = facturaExcel.OrdenCompra,
                                Recepcion = facturaExcel.Recepcion,
                                Identificador = llaveBusqueda,
                                Factura = facturaCfdi.Uuid,
                                Mensaje = $"Factura procesada exitosamente con la nota de crédito relacionada a la orden de compra {facturaExcel.OrdenCompra}."
                            });
                        }
                        else
                        {
                            _logger.LogError("Error al finalizar la factura con ID {IdFactura} con la nota de crédito relacionada a la orden de compra {OrdenCompra}. Error: {Message}", idFactura, facturaExcel.OrdenCompra, respuesta.Message);
                            noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                            {
                                OrdenCompra = facturaExcel.OrdenCompra,
                                Recepcion = facturaExcel.Recepcion,
                                Identificador = "error",
                                Factura = facturaCfdi.Uuid,
                                Mensaje = $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C} y ocurrió un error al finalizar la factura con la nota de crédito. Error: {respuesta.Message}"
                            });
                        }
                        continue;
                    }

                    //Si la factura es menor al monto de recepción se valida que no exceda el faltante permitido para el proveedor
                    if (totalFactura < montoRecepcion)
                    {
                        var faltante = montoRecepcion - totalFactura;

                        if (faltante > payloadProveedor!.Faltante)
                        {
                            noProcesados.Add(new ResultadoCargaFacturaGrupalDto
                            {
                                OrdenCompra = facturaExcel.OrdenCompra,
                                Recepcion = facturaExcel.Recepcion,
                                Identificador = llaveBusqueda,
                                Factura = facturaCfdi.Uuid,
                                Mensaje = $"La factura es menor al monto de la recepción por {faltante:C}, lo cual supera el faltante permitido para este proveedor."
                            });
                            continue;
                        }
                    }

                    // TODO: validar con PAC si el CFDI es correcto y está timbrado

                    // Se guardan los archivos en storage y se obtiene la url para guardar en la base de datos
                    // primero se guarda en storage
                    byte[] xmlFileBytes = ConvertirStreamABytes(streamXml);
                    var urlXmlFactura = await _storageService.UploadFilesAsync(new MemoryStream(xmlFileBytes, writable: false),
                        $"factura_{facturaCfdi.Uuid}_{Guid.NewGuid()}.xml", "xml");


                    string urlPdf = string.Empty;
                    if (pdfIndividual != null)
                    {
                        await using var pdfStream = pdfIndividual;
                        urlPdf = await _storageService.UploadFilesAsync(pdfStream, $"factura_{facturaCfdi.Uuid}_{Guid.NewGuid()}.pdf", "pdf");
                    }

                    //Se guarda factura con estatus finalizada
                    var idFacturaFinalizada = await GuardarFacturaAsync(
                        facturaCfdi,
                        payloadProveedor.IdProveedor,
                        1,
                        primeraRecepcion.IdRecepcion,
                        montoRecepcion,
                        urlXmlFactura,
                        urlPdf,
                        ordenCompraRecepcion.Data.Folio,
                        ordenCompraRecepcion.Data.Recepciones.FirstOrDefault()!.Folio,
                        string.Empty,
                        EstatusFacturaEnum.Procesada);


                    // Por simplicidad, aquí solo se marca como procesada sin hacer las validaciones ni guardados reales.
                    procesados.Add(new ResultadoCargaFacturaGrupalDto
                    {
                        OrdenCompra = facturaExcel.OrdenCompra,
                        Recepcion = facturaExcel.Recepcion,
                        Identificador = llaveBusqueda,
                        Factura = facturaCfdi.Uuid,
                        Mensaje = "Factura procesada exitosamente."
                    });

                }

            }

            var resultadoCargaMasiva = new CargaMasivaResponse()
            {
                Procesados = procesados,
                NoProcesados = noProcesados
            };

            //Se manda correo al proveedor con el resultado de la factura procesada
            var respuestaEnvioEmail = await EnvioEmailResultadoCargaMasiva(emailProveedor, resultadoCargaMasiva);

            await _notificacionesService.CrearNotificacionAsync(
                fecha: DateTime.Now,
                hora: DateTime.Now.TimeOfDay,
                titulo: "Resultado de Carga Masiva de Facturas",
                tag: "carga-masiva",
                detalle: "Se ha procesado la carga masiva de facturas.",
                usuarioIds: new List<long> { usuarioId }
            );

            return new ApiResponseDto<CargaMasivaResponse>
                {
                    Message = "Carga masiva de facturas procesada exitosamente.",
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Success = true,
                    Data = resultadoCargaMasiva
            };
        }
        catch (ApiProveedoresException err)
        {
            _logger.LogError("Se presento un error en el proceso de carga factura masiva {Error}", err.Message);
            return new ApiResponseDto<CargaMasivaResponse>
            {
                Message = err.Message,
                StatusCode = System.Net.HttpStatusCode.FailedDependency,
                Success = false,
                Data = new CargaMasivaResponse()
                {
                    Procesados = new List<ResultadoCargaFacturaGrupalDto>(),
                    NoProcesados = new List<ResultadoCargaFacturaGrupalDto>()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Error al procesar la carga masiva de facturas: {Error}", ex.Message);
            return new ApiResponseDto<CargaMasivaResponse>
            {
                Message = $"Error al procesar la carga masiva de facturas: {ex.Message}",
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Success = false,
                Data = new CargaMasivaResponse()
                {
                    Procesados = new List<ResultadoCargaFacturaGrupalDto>(),
                    NoProcesados = new List<ResultadoCargaFacturaGrupalDto>()
                }
            };
        }
    }

    public async Task<bool> ObtenerInformacionDashboardAsync(string email)
    {
        var proveedor = await _proveedoresService.RecuperaProveedorPorEmail(email);
        if (proveedor == null) {
            return false;
        }
        return true;
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

    private static (string? serie, string? folio, string? uuidRelacionado) ExtraerDoctoRelacionadoComplementoPago(byte[] xmlBytes)
    {
        // Importante: no convertir a string con UTF8 fijo; algunos XML llegan en UTF-16/BOM u otras variantes.
        // XmlReader detecta encoding con BOM/declaración y evita el error "Data at the root level is invalid".
        using var ms = new MemoryStream(xmlBytes);
        var settings = new System.Xml.XmlReaderSettings
        {
            DtdProcessing = System.Xml.DtdProcessing.Prohibit,
            IgnoreComments = true,
            IgnoreProcessingInstructions = false,
            IgnoreWhitespace = false
        };
        using var reader = System.Xml.XmlReader.Create(ms, settings);
        var doc = XDocument.Load(reader, System.Xml.Linq.LoadOptions.None);

        XNamespace pago20 = "http://www.sat.gob.mx/Pagos20";

        // Tomamos el primer DoctoRelacionado (si vienen varios, la relación puede ser múltiple)
        var docto = doc
            .Descendants(pago20 + "DoctoRelacionado")
            .FirstOrDefault();

        if (docto == null)
            return (null, null, null);

        var serie = (string?)docto.Attribute("Serie");
        var folio = (string?)docto.Attribute("Folio");
        var uuidRel = (string?)docto.Attribute("IdDocumento");
        return (serie, folio, uuidRel);
    }

    public async Task<ValidacionFacturaResponseDto<bool>> ProcesaCargaComplementoPagoAsync(
        int idCliente,
        IFormFile xml,
        IFormFile? pdf)
    {
        _logger.LogInformation("Iniciando carga de complemento de pago para proveedor {IdCliente}.", idCliente);

        if (xml == null || xml.Length == 0)
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = "Se requiere un archivo XML (Complemento de pago).",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
                Data = false
            };

        if (idCliente <= 0)
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = "ID de cliente requerido.",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false,
                Data = false
            };

        try
        {
            var cliente = await _usuariosService.ObtenerUsuarioPorIdAsync(idCliente);
            if (cliente == null)
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = "Cliente no encontrado.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                    Data = false
                };

            var proveedor = await _proveedoresService.ObtenerProveedorPorRfcAsync(cliente.RfcProveedor);

            if (proveedor == null)
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"No se encontró un proveedor con el RFC {cliente.RfcProveedor}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                    Data = false
                };

            await using var xmlReadStream = xml.OpenReadStream();
            using var xmlMem = new MemoryStream();
            await xmlReadStream.CopyToAsync(xmlMem);
            var xmlBytes = xmlMem.ToArray();

            var cfdi = ObtenerFacturaDesdeXml(new MemoryStream(xmlBytes, writable: false));

            // TODO: validar con PAC si el CFDI es correcto y está timbrado

            if (!string.Equals(cfdi.TipoDeComprobante, "P", StringComparison.OrdinalIgnoreCase))
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"El XML no corresponde a un Complemento de pago (TipoDeComprobante=P). Tipo recibido: {cfdi.TipoDeComprobante ?? "(vacío)"}",
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false,
                    Data = false
                };

            // Validación básica: el RFC del emisor del XML debe corresponder al proveedor que carga
            if (!string.IsNullOrWhiteSpace(cfdi.RfcEmisor) &&
                !string.Equals(cfdi.RfcEmisor.Trim(), cliente.RfcProveedor.Trim(), StringComparison.OrdinalIgnoreCase))
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"El RFC emisor del XML ({cfdi.RfcEmisor}) no coincide con el RFC del proveedor ({cliente.RfcProveedor}).",
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false,
                    Data = false
                };

            var (serieRel, folioRel, uuidRelacionado) = ExtraerDoctoRelacionadoComplementoPago(xmlBytes);

            // Se valida que exista la factura relacionada por UUID o por serie y folio, y que esté asociada a una recepción del proveedor
            var facturaRelacionada = await _db.Facturas
                .AsNoTracking()
                .Where(f => f.RfcProveedor == cliente.RfcProveedor &&
                            ((uuidRelacionado != null && f.Uuid == uuidRelacionado) ||
                             (serieRel != null && folioRel != null && f.Serie == serieRel && f.Folio == folioRel)) &&
                            f.NoRecepcion != null)
                .FirstOrDefaultAsync();

            if( facturaRelacionada == null)
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"No se encontró una factura relacionada que coincida con el UUID ({uuidRelacionado}) o Serie/Folio ({serieRel}/{folioRel}) y que esté asociada a una recepción para el RFC {cliente.RfcProveedor}.",
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false,
                    Data = false
                };

            // Guardado del complemento de pago en tablas dedicadas:
            // pagos_cfdi -> pagos_detalle -> pagos_facturas_relacionadas
            var uuid = cfdi.Uuid ?? Guid.NewGuid().ToString();

            // Si ya existe el complemento por UUID, evitamos duplicados.
            var existe = await _db.PagosCfdi.AsNoTracking().AnyAsync(p => p.Uuid == uuid);
            if (existe)
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = $"El complemento de pago con UUID {uuid} ya fue registrado previamente.",
                    StatusCode = System.Net.HttpStatusCode.Conflict,
                    Success = false,
                    Data = false
                };

            // Subir XML a storage (se guarda la URL en pagos_cfdi.xml_original)
            var urlXml = await _storageService.UploadFilesAsync(
                new MemoryStream(xmlBytes, writable: false),
                $"complemento_pago_{uuid}_{Guid.NewGuid()}.xml",
                "xml");

            // Subir PDF a storage (si viene). Por ahora no se persiste porque el modelo no trae columna.
            if (pdf != null && pdf.Length > 0)
            {
                await using var pdfStream = pdf.OpenReadStream();
                _ = await _storageService.UploadFilesAsync(
                    pdfStream,
                    $"complemento_pago_{uuid}_{Guid.NewGuid()}.pdf",
                    "pdf");
            }

            // Cargar XML respetando encoding (BOM/declaración)
            using var ms = new MemoryStream(xmlBytes, writable: false);
            var settings = new System.Xml.XmlReaderSettings
            {
                DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreProcessingInstructions = false,
                IgnoreWhitespace = false
            };
            using var reader = System.Xml.XmlReader.Create(ms, settings);
            var xdoc = XDocument.Load(reader, System.Xml.Linq.LoadOptions.None);

            XNamespace cfdiNs = "http://www.sat.gob.mx/cfd/4";
            XNamespace pago20 = "http://www.sat.gob.mx/Pagos20";
            XNamespace tfd = "http://www.sat.gob.mx/TimbreFiscalDigital";

            var comprobante = xdoc.Root;
            var emisor = comprobante?.Element(cfdiNs + "Emisor");
            var receptor = comprobante?.Element(cfdiNs + "Receptor");

            var timbreUuid = xdoc
                .Descendants(tfd + "TimbreFiscalDigital")
                .Select(x => (string?)x.Attribute("UUID"))
                .FirstOrDefault();

            // Preferimos UUID del timbre si está.
            if (!string.IsNullOrWhiteSpace(timbreUuid))
                uuid = timbreUuid.Trim();

            var fechaComprobante = (DateTime?)cfdi.FechaComprobante ?? DateTime.UtcNow;

            var pagoCfdi = new PagoCfdi
            {
                Uuid = uuid,
                Serie = cfdi.Comprobante.Serie ?? string.Empty,
                Folio = cfdi.Comprobante.Folio ?? string.Empty,
                Fecha = EnsureUtc(fechaComprobante) ?? DateTime.UtcNow,
                RfcEmisor = cfdi.RfcEmisor ?? (string?)emisor?.Attribute("Rfc") ?? string.Empty,
                NombreEmisor = cfdi.NombreEmisor ?? (string?)emisor?.Attribute("Nombre") ?? string.Empty,
                RfcReceptor = cfdi.RfcReceptor ?? (string?)receptor?.Attribute("Rfc") ?? string.Empty,
                NombreReceptor = cfdi.NombreReceptor ?? (string?)receptor?.Attribute("Nombre") ?? string.Empty,
                Total = cfdi.Total ?? 0m,
                XmlOriginal = urlXml,
                FechaAlta = DateTime.UtcNow
            };

            // Tomamos el primer nodo Pago (si vienen varios, se puede extender a 1..N)
            var pagoNodo = xdoc.Descendants(pago20 + "Pago").FirstOrDefault();
            if (pagoNodo == null)
                return new ValidacionFacturaResponseDto<bool>
                {
                    Message = "El XML no contiene el nodo pago20:Pago requerido en el Complemento de pago (Pagos 2.0).",
                    StatusCode = System.Net.HttpStatusCode.BadRequest,
                    Success = false,
                    Data = false
                };

            static decimal ParseDecimalAttr(XElement el, string attr, decimal @default = 0m)
            {
                var s = (string?)el.Attribute(attr);
                if (string.IsNullOrWhiteSpace(s)) return @default;
                return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : @default;
            }

            static int ParseIntAttr(XElement el, string attr, int @default = 0)
            {
                var s = (string?)el.Attribute(attr);
                if (string.IsNullOrWhiteSpace(s)) return @default;
                return int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : @default;
            }

            static DateTime ParseDateTimeAttr(XElement el, string attr, DateTime @default)
            {
                var s = (string?)el.Attribute(attr);
                if (string.IsNullOrWhiteSpace(s)) return @default;
                return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var v) ? v : @default;
            }

            var pagoDetalle = new ApiProveedores.Models.ComplementoPago.PagoDetalle
            {
                // PagoCfdiId se asigna después de guardar PagoCfdi
                FechaPago = EnsureUtc(ParseDateTimeAttr(pagoNodo, "FechaPago", DateTime.UtcNow)) ?? DateTime.UtcNow,
                FormaPago = (string?)pagoNodo.Attribute("FormaDePagoP") ?? string.Empty,
                Moneda = (string?)pagoNodo.Attribute("MonedaP") ?? string.Empty,
                TipoCambio = ParseDecimalAttr(pagoNodo, "TipoCambioP", 1m),
                Monto = ParseDecimalAttr(pagoNodo, "Monto", 0m),
                NumeroOperacion = (string?)pagoNodo.Attribute("NumOperacion") ?? string.Empty,
                BancoOrdenante = (string?)pagoNodo.Attribute("NomBancoOrdExt") ?? string.Empty,
                CuentaOrdenante = (string?)pagoNodo.Attribute("CtaOrdenante") ?? string.Empty,
                CuentaBeneficiario = (string?)pagoNodo.Attribute("CtaBeneficiario") ?? string.Empty
            };

            var doctos = pagoNodo
                .Descendants(pago20 + "DoctoRelacionado")
                .ToList();

            await using var tx = await _db.Database.BeginTransactionAsync();

            await _db.PagosCfdi.AddAsync(pagoCfdi);
            await _db.SaveChangesAsync();

            pagoDetalle.PagoCfdiId = pagoCfdi.Id;
            await _db.PagosDetalle.AddAsync(pagoDetalle);
            await _db.SaveChangesAsync();

            if (doctos.Count > 0)
            {
                var relaciones = doctos.Select(d => new PagosFacturas
                {
                    PagoId = pagoDetalle.Id,
                    UuidFactura = (string?)d.Attribute("IdDocumento") ?? string.Empty,
                    Serie = (string?)d.Attribute("Serie") ?? string.Empty,
                    Folio = (string?)d.Attribute("Folio") ?? string.Empty,
                    NumeroParcialidad = ParseIntAttr(d, "NumParcialidad", 0),
                    ImporteSaldoAnterior = ParseDecimalAttr(d, "ImpSaldoAnt", 0m),
                    ImportePagado = ParseDecimalAttr(d, "ImpPagado", 0m),
                    ImporteSaldoInsoluto = ParseDecimalAttr(d, "ImpSaldoInsoluto", 0m)
                }).ToList();

                await _db.PagosFacturasRelacionadas.AddRangeAsync(relaciones);
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();

            return new ValidacionFacturaResponseDto<bool>
            {
                Accion = TipoAccionSiguientejEnum.ProcesoCompleto,
                Message = "Complemento de pago capturado correctamente.",
                StatusCode = System.Net.HttpStatusCode.OK,
                Success = true,
                Data = true,
                ProcesoId = pagoCfdi.Id.ToString(CultureInfo.InvariantCulture)
            };
        }
        catch (ApiProveedoresException ex)
        {
            _logger.LogError("Error en carga de complemento de pago: {Message}", ex.Message);
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
            _logger.LogError("Error inesperado en carga de complemento de pago: {Message}", ex.Message);
            return new ValidacionFacturaResponseDto<bool>
            {
                Message = ex.Message,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Success = false,
                Data = false
            };
        }
    }

    public async Task<ResultadoPaginado<ComplementoPagoConsultaDto>> ObtieneComplementosPago(int pagina, int tamanioPagina, DateTime? fechaInicial, DateTime? fechaFinal, int idCliente, string? textoBusqueda = null)
    {
        try
        {
            _logger.LogInformation("Obteniendo complementos de pago para cliente {IdCliente}.", idCliente);
            var cliente = await _usuariosService.ObtenerUsuarioPorIdAsync(idCliente);
            if (cliente == null)
                throw new Exception("El cliente no fue localizado");

            var baseQuery =
                from pc in _db.PagosCfdi.AsNoTracking()
                join pd in _db.PagosDetalle.AsNoTracking() on pc.Id equals pd.PagoCfdiId
                join pf in _db.PagosFacturasRelacionadas.AsNoTracking() on pd.Id equals pf.PagoId
                join prov in _db.Proveedores.AsNoTracking() on pc.RfcEmisor equals prov.Rfc into provJoin
                from prov in provJoin.DefaultIfEmpty()
                where pc.RfcEmisor == cliente.RfcProveedor
                select new { pc, pd, pf, prov };

            if (fechaInicial.HasValue)
            {
                var fi = fechaInicial.Value.ToUniversalTime();
                baseQuery = baseQuery.Where(x => x.pc.FechaAlta >= fi);
            }

            if (fechaFinal.HasValue)
            {
                var ff = fechaFinal.Value.ToUniversalTime();
                baseQuery = baseQuery.Where(x => x.pc.FechaAlta <= ff);
            }

            // Un solo texto: coincide con UUID del complemento de pago o UUID de factura relacionada (sin saber cuál viene del front).
            if (!string.IsNullOrWhiteSpace(textoBusqueda))
            {
                var term = textoBusqueda.Trim();
                baseQuery = baseQuery.Where(x =>
                    (x.pc.Uuid != null && EF.Functions.ILike(x.pc.Uuid, term)) ||
                    (x.pf.UuidFactura != null && EF.Functions.ILike(x.pf.UuidFactura, term)));
            }

            var totalElementos = await baseQuery.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalElementos / (double)tamanioPagina);

            var complementos = await baseQuery
                .OrderByDescending(x => x.pc.FechaAlta)
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .Select(x => new ComplementoPagoConsultaDto
                {
                    IdComplementoPago = x.pc.Id,
                    FechaAlta = x.pc.FechaAlta,
                    UuidPago = x.pc.Uuid,
                    FechaPago = x.pd.FechaPago,
                    FormaPago = x.pd.FormaPago,
                    Moneda = x.pd.Moneda,
                    TipoCambioPago = x.pd.TipoCambio,
                    MontoPago = x.pd.Monto,
                    UuidFactura = x.pf.UuidFactura,
                    SerieDocumentoRelacionado = x.pf.Serie,
                    FolioDocumentoRelacionado = x.pf.Folio,
                    MetodoPago = null, // No se persiste en las tablas actuales (SAT: MetodoDePagoDR/MetodoPago).
                    NumeroParcialidad = x.pf.NumeroParcialidad,
                    TipoDocumento = "P",
                    RfcProveedor = x.pc.RfcEmisor,
                    IdProveedor = x.prov != null ? x.prov.Id_proveedor : 0,
                    NumeroOperacion = x.pd.NumeroOperacion
                })
                .ToListAsync();

            return new ResultadoPaginado<ComplementoPagoConsultaDto>
            {
                TotalElementos = totalElementos,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                Elementos = complementos
            };
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
        
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

    private ApiResponseDto<CargaMasivaResponse> ValidarArchivosCargaMasiva(IFormFile listadoFacturasExcel, IFormFile archivoZip, string rfcProveedor)
    {
        _logger.LogInformation("Iniciando proceso de carga masiva de facturas para el proveedor con RFC {RfcProveedor}.", rfcProveedor);
        if (listadoFacturasExcel == null || listadoFacturasExcel.Length == 0)
        {
            return new ApiResponseDto<CargaMasivaResponse>
            {
                Message = "Archivo de listado de facturas no proporcionado.",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false
            };
        }

        if (archivoZip == null || archivoZip.Length == 0)
        {
            return new ApiResponseDto<CargaMasivaResponse>
            {
                Message = "Archivo ZIP con facturas no proporcionado.",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false
            };
        }

        if (!Path.GetExtension(listadoFacturasExcel.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResponseDto<CargaMasivaResponse>
            {
                Message = "El archivo de listado de facturas debe ser un archivo Excel (.xlsx).",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false
            };
        }

        if (!Path.GetExtension(archivoZip.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return new ApiResponseDto<CargaMasivaResponse>
            {
                Message = "El archivo con facturas debe ser un archivo ZIP (.zip).",
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Success = false
            };
        }

        return new ApiResponseDto<CargaMasivaResponse>
        {
            Message = "Validación de archivos exitosa.",
            StatusCode = System.Net.HttpStatusCode.OK,
            Success = true
        };
    }

    private async Task<bool> EnvioEmailResultadoCargaMasiva(string emailProveedor, CargaMasivaResponse resultadoCarga)
    {
        try
        {
            _logger.LogInformation("Enviando correo electrónico a {EmailProveedor} con el resultado de la carga masiva.", emailProveedor);
            var json = JsonSerializer.Serialize(resultadoCarga, new JsonSerializerOptions { WriteIndented = true });
            var bodyHtml = await _emailHelper.GeneraBodyResultadoCargaFacturaMasiva(resultadoCarga, "EmailTemplates:ResultadoCargaFacturaMasiva");

            var mensajeNuevo = new NotificacionEmail
            {
                ClaveAplicativo = "app-portal-proveedores",
                NombreTemplate = "carga_masiva",
                EmailDestino = emailProveedor,
                Data = resultadoCarga
            };

            await _pubSubService.EnviarNotificacionAsync(mensajeNuevo);

            _logger.LogInformation("Correo electrónico enviado exitosamente a {EmailProveedor} con el resultado de la carga masiva.", emailProveedor);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error al enviar el correo electrónico a {EmailProveedor} con el resultado de la carga masiva: {Message}", emailProveedor, ex.Message);
            return false;
        }
    }

    public IFormFile ConvertStreamToIFormFile(Stream stream, string fileName)
    {
        var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream" 
        };

        return formFile;
    }
}

using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Proveedor;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;
using ApiProveedores.Models.Enum;
using ApiProveedores.Models.Factura;
using ApiProveedores.Services.PubSub;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office.PowerPoint.Y2021.M06.Main;

namespace ApiProveedores.Services.Reportes
{
    public class ReporteService
    {
        private readonly PortalDbContext _db;
        private readonly ILogger<ReporteService> _logger;
        private readonly PublisherPnjService _publisherPnjService;
        private readonly StorageService _storageService;
        private readonly ProveedoresService _proveedoresService;
        private readonly OrdenCompraService _ordenCompraService;
        private readonly NotificacionesService _notificacionesService;
        private readonly FacturaService _facturaService;
        private readonly string _tiempoExpiracionUrl;

        public ReporteService(PortalDbContext db, ILogger<ReporteService> logger, PublisherPnjService publisherPnjService, StorageService storageService, ProveedoresService proveedoresService, OrdenCompraService ordenCompraService, NotificacionesService notificacionesService, FacturaService facturaService, IConfiguration config)
        {
            _db = db;
            _logger = logger;
            _publisherPnjService = publisherPnjService;
            _storageService = storageService;
            _proveedoresService = proveedoresService;
            _ordenCompraService = ordenCompraService;
            _notificacionesService = notificacionesService;
            _facturaService = facturaService;
            _tiempoExpiracionUrl = config["GCP:TiempoExpiracionUrl"] ?? "45";
        }


        public async Task<ApiResponseDto<bool>> GenerarReporteAsync(CriterioReporte criterio, long usuarioId)
        {
            try
            {
                var proveedor = await _proveedoresService.ObtenerInfoProveedorPorRfcAsync(criterio.Rfc);
                var payloadProveedor = proveedor.Values.OfType<ProveedorResponseDto>().FirstOrDefault();

                if (payloadProveedor == null)
                    return new ApiResponseDto<bool>
                    {
                        Message = $"No se encontró un proveedor con el RFC {criterio.Rfc}.",
                        StatusCode = System.Net.HttpStatusCode.NotFound,
                        Success = false
                    };

                if (payloadProveedor.Email is null)
                {
                    _logger.LogWarning("El proveedor con RFC {RfcProveedor} no tiene un correo electrónico registrado para enviar la notificación.", criterio.Rfc);
                    return new ApiResponseDto<bool>
                    {
                        Message = $"El proveedor con RFC {criterio.Rfc} no tiene un correo electrónico registrado para enviar la notificación.",
                        StatusCode = System.Net.HttpStatusCode.BadRequest,
                        Success = false
                    };
                }

                var urlReporte = string.Empty;

                switch (criterio.TipoDocumento)
                {
                    case TipoDocumento.Recepcion:
                        urlReporte = await GeneraReporteRecepcionesSinFacturaAsync(criterio.FechaInicio, criterio.FechaFinal, criterio.Rfc);
                        break;
                    case TipoDocumento.Documentos:
                        urlReporte = await GeneraReporteDocumentosCargadosAsync(criterio);
                        break;
                    default:
                        break;
                }

                var mensajeEmail = new NotificacionEmail
                {
                    ClaveAplicativo = "app-portal-proveedores",
                    NombreTemplate = "reporte_generado",
                    EmailDestino = payloadProveedor.Email ?? string.Empty,
                    Data = new ReporteGeneradoDto
                    {
                        Nombre = payloadProveedor.Nombre ?? "Proveedor",
                        Url = urlReporte
                    }
                };

                await _publisherPnjService.EnviarNotificacionAsync(mensajeEmail);

                var notificacion = await _notificacionesService.CrearNotificacionAsync(
                fecha: DateTime.Now,
                hora: DateTime.Now.TimeOfDay,
                titulo: "Resultado de reporte generado",
                tag: "reporte-generado",
                detalle: "Se ha procesado la carga masiva de facturas.",
                usuarioIds: new List<long> { usuarioId }
                 );

                if (notificacion == 0)
                {
                    _logger.LogWarning("No se pudo crear la notificación para el proveedor con RFC {RfcProveedor}.", criterio.Rfc);
                }

                return new ApiResponseDto<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Reporte generado exitosamente."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar el reporte.");
                return new ApiResponseDto<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Error al generar el reporte: {ex.Message}"
                };
            }
        }

        private async Task<string> GeneraReporteRecepcionesSinFacturaAsync(DateTime? fechaInicial, DateTime? fechaFinal, string rfcProveedor)
        {
            try
            {
                var recepcionesSinFactura = await _ordenCompraService.GetRecepcionesSinFacturaAsync(rfcProveedor, fechaInicial, fechaFinal);

                if (recepcionesSinFactura.Data is null)
                {
                    _logger.LogWarning("No se encontraron recepciones sin factura para el proveedor con RFC {RfcProveedor} en el rango de fechas proporcionado.", rfcProveedor);
                    return string.Empty;
                }

                using var archivoExcel = await GeneraArchivoExcelReporteRecepciones(recepcionesSinFactura.Data);

                var nombreArchivo = $"Reporte_Recepciones_Sin_Factura_{rfcProveedor}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                if (archivoExcel.Length == 0)
                {
                    _logger.LogWarning("El archivo Excel generado para el reporte de recepciones sin factura está vacío para el proveedor con RFC {RfcProveedor}.", rfcProveedor);
                    return string.Empty;
                }

                var urlArchivo = await _storageService.UploadFilesAsync(archivoExcel, nombreArchivo, "reportes/recepciones_sin_factura", true);

                var urlFirmada = await _storageService.GenerateSignedUrlAsync(urlArchivo, TimeSpan.FromMinutes(int.Parse(_tiempoExpiracionUrl)));

                return urlFirmada;

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private async Task<string> GeneraReporteDocumentosCargadosAsync(CriterioReporte criterio)
        {
            try
            {
                var documentosCargados = await _facturaService.ConsultarFacturasCompletoAsync(criterio.Rfc, criterio.TiposDocumentos.Ingreso, criterio.TiposDocumentos.Egreso, criterio.TiposDocumentos.Complemento, criterio.FechaInicio, criterio.FechaFinal);

                if (!documentosCargados.Success || documentosCargados.Data == null)
                {
                    _logger.LogWarning("No se encontraron recepciones sin factura para el proveedor con RFC {RfcProveedor} en el rango de fechas proporcionado.", criterio.Rfc);
                    return string.Empty;
                }

                using var archivoExcel = await GeneraArchivoExcelReporteDocumentosProcesados(documentosCargados.Data);

                var nombreArchivo = $"Reporte_Documentos_Procesados_{criterio.Rfc}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                if (archivoExcel.Length == 0)
                {
                    _logger.LogWarning("El archivo Excel generado para el reporte de recepciones sin factura está vacío para el proveedor con RFC {RfcProveedor}.", criterio.Rfc);
                    return string.Empty;
                }

                var urlArchivo = await _storageService.UploadFilesAsync(archivoExcel, nombreArchivo, "reportes/recepciones_sin_factura", true);

                var urlFirmada = await _storageService.GenerateSignedUrlAsync(urlArchivo, TimeSpan.FromMinutes(int.Parse(_tiempoExpiracionUrl)));

                return urlFirmada;

            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }

        private async Task<MemoryStream> GeneraArchivoExcelReporteRecepciones(List<Recepcion> recepciones)
        {
            try
            {
                using var workbook = new XLWorkbook();

                var worksheet = workbook.Worksheets.Add("RecepcionesSinFactura");

                worksheet.Cell(1, 1).Value = "Id Recepcion";
                worksheet.Cell(1, 2).Value = "Folio";
                worksheet.Cell(1, 3).Value = "Total";
                worksheet.Cell(1, 4).Value = "Cantidad";
                worksheet.Cell(1, 5).Value = "Proveedor";

                var headerRange = worksheet.Range(1, 1, 1, 5);

                headerRange.Style.Font.Bold = true;
                var row = 2;

                foreach (var recepcion in recepciones)
                {
                    worksheet.Cell(row, 1).Value = recepcion.IdRecepcion;
                    worksheet.Cell(row, 2).Value = recepcion.Folio ?? "N/A";
                    worksheet.Cell(row, 3).Value = recepcion.Total ?? 0;
                    worksheet.Cell(row, 4).Value = recepcion.Cantidad ?? 0;
                    worksheet.Cell(row, 5).Value = recepcion.ProveedorNombre ?? "N/A";

                    row++;
                }

                worksheet.Columns().AdjustToContents();

                var stream = new MemoryStream();

                workbook.SaveAs(stream);

                stream.Position = 0;

                return stream;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task<MemoryStream> GeneraArchivoExcelReporteDocumentosProcesados(List<Factura> documentos)
        {
            try
            {
                using var workbook = new XLWorkbook();

                var worksheet = workbook.Worksheets.Add("DocumentosProcesados");

                worksheet.Cell(1, 1).Value = "UUID";
                worksheet.Cell(1, 2).Value = "Total";
                worksheet.Cell(1, 3).Value = "Orden de Compra";
                worksheet.Cell(1, 4).Value = "Recepción";
                worksheet.Cell(1, 5).Value = "RFC Proveedor";

                var headerRange = worksheet.Range(1, 1, 1, 5);

                headerRange.Style.Font.Bold = true;
                var row = 2;

                foreach (var documento in documentos)
                {
                    worksheet.Cell(row, 1).Value = documento.Uuid;
                    worksheet.Cell(row, 2).Value = documento.Total;
                    worksheet.Cell(row, 3).Value = documento.NoOrdenCompra ?? "N/A";
                    worksheet.Cell(row, 4).Value = documento.NoRecepcion ?? "N/A";
                    worksheet.Cell(row, 5).Value = documento.RfcProveedor ?? "N/A";

                    row++;
                }

                worksheet.Columns().AdjustToContents();

                var stream = new MemoryStream();

                workbook.SaveAs(stream);

                stream.Position = 0;

                return stream;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}

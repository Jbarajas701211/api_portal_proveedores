using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using ApiProveedores.Dto.Proveedor;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Models.Factura;
using ApiProveedores.Models.Config;
using ApiProveedores.Services.Exceptions;
using Google.Cloud.PubSub.V1;

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

    public FacturaService(OrdenCompraService ordenCompraService, ProveedoresService proveedoresService, PortalDbContext db)
    {
        _ordenCompraService = ordenCompraService;
        _proveedoresService = proveedoresService;
        _db = db;
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

    public async Task<ValidacionFacturaResponseDto> ProcesaCargaFactura(string rfcProveedor, string folioOrdenCompra, string folioRecibo, IFormFile[] file)
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

        FacturaCfdiDocumento factura;

        try
        {
            using var xmlStream = xmlFile.OpenReadStream();

            factura = ObtenerFacturaDesdeXml(xmlStream);

            var recepcion = await _ordenCompraService.ObtenerRecepcionesPorIdOcAsync(folioRecibo);

            if (recepcion == null)
                return new ValidacionFacturaResponseDto
                {
                    Message = $"No se encontró una recepción con el folio {folioRecibo}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                };

            if(recepcion.FirstOrDefault() != null && factura.Total > recepcion.FirstOrDefault()?.Monto)
            {
                var proveedor = await _proveedoresService.ObtenerInfoProveedorPorRfcAsync(rfcProveedor);

                if(proveedor == null)
                    return new ValidacionFacturaResponseDto
                    {
                        Message = $"No se encontró un proveedor con el RFC {rfcProveedor}.",
                        StatusCode = System.Net.HttpStatusCode.NotFound,
                        Success = false,
                    };
                var diccionarioProveedor = (ProveedorResponseDto)proveedor["payload"];

                var diferenciaFacturaVsRecepcion = factura.Total - recepcion.FirstOrDefault()?.Monto;

                if (diferenciaFacturaVsRecepcion > diccionarioProveedor.Sobrante)
                {
                    //Primero se guarda la factura para obtener su id



                    return new ValidacionFacturaResponseDto
                    {
                        Message = $"La factura excede el monto de la recepción por {diferenciaFacturaVsRecepcion:C}, lo cual supera el sobrante permitido para este proveedor.",
                        StatusCode = System.Net.HttpStatusCode.Accepted,
                        Success = false,
                    };
                }


            }

        }
        catch (Exception)
        {

            throw;
        }
        //var factura = ObtenerFacturaDesdeXml(xmlStream);
        //// Aquí se podrían agregar validaciones adicionales (ej. RFC coincide con el proveedor, etc.)
        //// Simulación de procesamiento y publicación en Pub/Sub
        //var publisher = await PublisherClient.CreateAsync("proyecto-id", "topic-id");
        //var mensaje = new PubsubMessage
        //{
        //    Data = Google.Protobuf.ByteString.CopyFromUtf8($"Factura procesada: {factura.Folio} del proveedor {rfcProveedor}")
        //};
        //await publisher.PublishAsync(mensaje);
        //return new ApiResponseDto
        //{
        //    Success = true,
        //    Message = "Factura procesada y mensaje publicado en Pub/Sub."
        //};
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
}

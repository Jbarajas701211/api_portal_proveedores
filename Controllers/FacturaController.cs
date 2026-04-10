using ApiProveedores.Models.Factura;
using ApiProveedores.Services;
using ApiProveedores.Services.PubSub;
using ApiProveedores.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/facturas")]
    public class FacturaController : ControllerBase
    {
        private readonly StorageService _storageService;
        private readonly FacturaService _facturaService;

        public FacturaController(StorageService storageService, FacturaService facturaService)
        {
            _storageService = storageService;
            _facturaService = facturaService;
        }

        [HttpGet("signed_url")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSignedUrl([FromQuery] string objectUrl, [FromQuery] int expiryMinutes = 15)
        {
            if (string.IsNullOrWhiteSpace(objectUrl))
                return BadRequest(new { mensaje = "URL de objeto inválida." });

            if (expiryMinutes <= 0)
                expiryMinutes = 15;

            try
            {
                var signed = await _storageService.GenerateSignedUrlAsync(objectUrl, TimeSpan.FromMinutes(expiryMinutes));
                return Ok(new { signed_url = signed, expires_in_minutes = expiryMinutes });
            }
            catch (ApiProveedoresException appEx)
            {
                return BadRequest(new { mensaje = appEx.Message ?? "No se pudo generar la URL firmada." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "Error interno al generar la URL firmada." });
            }
        }

        [HttpPost("alta_de_factura")]
        public async Task<IActionResult> UploadFactura(IFormFile[] file, [FromQuery] string rfcProveedor, string folioOrdenCompra, string folioRecibo)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { mensaje = "Archivo no proporcionado." });

            var archivos = file.Where(f => f != null && f.Length > 0).ToList();
            if (archivos.Count == 0)
                return BadRequest(new { mensaje = "Ningún archivo tiene contenido." });

            var xmlFile = archivos.FirstOrDefault(EsArchivoXmlFactura);
            if (xmlFile == null)
                return BadRequest(new { mensaje = "Se requiere un archivo XML de factura (CFDI)." });

            FacturaCfdiDocumento factura;
            try
            {
                using var xmlStream = xmlFile.OpenReadStream();
                factura = _facturaService.ObtenerFacturaDesdeXml(xmlStream);
            }
            catch (ApiProveedoresException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }

            // Validaciones de negocio sobre `factura`

            try
            {
                var nombresSubidos = new List<string>();
                foreach (var doc in archivos)
                {
                    using var stream = doc.OpenReadStream();
                    var fileName = $"{Guid.NewGuid()}_{doc.FileName}";
                    var uploadedFileName = await _storageService.UploadFilesAsync(stream, fileName);
                    nombresSubidos.Add(uploadedFileName);
                }

                return Ok(new
                {
                    mensaje = "Archivos subidos correctamente.",
                    rfcProveedor,
                    uuid = factura.Uuid,
                    serie = factura.Comprobante.Serie,
                    folio = factura.Comprobante.Folio,
                    rfcEmisor = factura.RfcEmisor,
                    rfcReceptor = factura.RfcReceptor,
                    total = factura.Total,
                    archivos = nombresSubidos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al subir el archivo: {ex.Message}" });
            }
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
}

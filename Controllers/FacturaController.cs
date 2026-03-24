using ApiProveedores.Services.PubSub;
using ApiProveedores.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/facturas")]
    public class FacturaController : ControllerBase
    {
        private readonly StorageService _storageService;

        public FacturaController(StorageService storageService)
        {
            _storageService = storageService;
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

        [HttpPost]
        public async Task<IActionResult> UploadFactura(IFormFile[] file, [FromQuery] string idProveedor)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { mensaje = "Archivo no proporcionado." });
            try
            {
                string uploadedFileName = string.Empty;
                foreach (IFormFile doc in file)
                {
                    using var stream = doc.OpenReadStream();
                    var fileName = $"{Guid.NewGuid()}_{doc.FileName}";
                    uploadedFileName = await _storageService.UploadFilesAsync(stream, fileName);
                }
                
                return Ok(new { mensaje = "Archivo subido correctamente.", fileName = uploadedFileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al subir el archivo: {ex.Message}" });
            }
        }


    }
}

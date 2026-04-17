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

        [HttpGet("consultar_facturas")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFacturasAsync(
            [FromQuery] int pagina = 1, 
            [FromQuery] int tamanioPagina = 10, 
            [FromQuery] DateTime? fechaInicial = null, 
            [FromQuery] DateTime? fechaFinal = null,
            [FromQuery] string? estatus = null
            )
        {
            var resultado = await _facturaService.ConsultarFacturasAsync(pagina, tamanioPagina, fechaInicial, fechaFinal, estatus);
            return Ok(resultado);
        }

        [HttpGet("consultar_facturas_proveedor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFacturasProveedorAsync(
            [FromQuery] long idProveedor,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 10,
            [FromQuery] DateTime? fechaInicial = null,
            [FromQuery] DateTime? fechaFinal = null,
            [FromQuery] string? estatus = null
            )
        {
            var resultado = await _facturaService.ConsultarFacturasAsync(pagina, tamanioPagina, fechaInicial, fechaFinal, estatus);
            return Ok(resultado);
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
        public async Task<IActionResult> UploadFactura(IFormFile[] file, [FromQuery] string rfcProveedor, string folioOrdenCompra, string folioRecibo, long empresaId)
        {

            try
            {
                var response = await _facturaService.ProcesaCargaFactura(rfcProveedor, folioOrdenCompra, folioRecibo, file, empresaId);
                return Ok(response);
            }
            catch (ApiProveedoresException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        [HttpPost("finalizar_con_nota")]
        public async Task<IActionResult> FinalizarConNota(IFormFile[] files, [FromQuery] long procesoId, string motivo)
        {
            try
            {
                var response = await _facturaService.FinalizarFacturaConNotaAsync(files, procesoId, motivo);
                return Ok(response);
            }
            catch (ApiProveedoresException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }

        }
    }
}

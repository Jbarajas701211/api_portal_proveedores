using ApiProveedores.Dto.Catalogos;
using ApiProveedores.Dto.Entrada;
using ApiProveedores.Services;
using ApiProveedores.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/orden_compra")]
    public class OrdenCompraController : ControllerBase
    {
        private readonly OrdenCompraService _service;

        public OrdenCompraController(OrdenCompraService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar(
            [FromQuery] string? filtro,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanio = 10)
        {
            var resultado = await _service.BuscarProveedoresPaginadoAsync(filtro, pagina, tamanio);
            return Ok(resultado);
        }

        [Authorize]
        [HttpGet("tiene_ordenes_compra")]
        public async Task<IActionResult> ValidaSiCuentaConOrdenesCompraSinFactura([FromQuery] string idProveedor)
        {
            try
            {
                var resultado = await _service.ValidaSiCuentaConOrdenesCompraSinFactura(idProveedor);
                return Ok(new { tieneOrdenesCompraSinFactura = resultado });
            }
            catch (ApiProveedoresException appEx)
            {
                return BadRequest(new { mensaje = appEx.Message ?? "No se pudo validar las órdenes de compra pendientes de facturar." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "No se pudo validar las órdenes de compra pendientes de facturar." });
            }

        }

        [Authorize]
        [HttpGet("ordenes_compra_sin_factura")]
        public async Task<IActionResult> ObtenerOrdenesCompraSinFactura([FromQuery] string idProveedor)
        {
            try
            {
                var resultado = await _service.GetOrdenesSinFacturaAsync(idProveedor);
                return Ok(resultado);
            }
            catch (ApiProveedoresException appEx)
            {
                return BadRequest(new { mensaje = appEx.Message ?? "No se pudieron obtener las órdenes de compra pendientes de facturar." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "No se pudieron obtener las órdenes de compra pendientes de facturar." });
            }
        }
    }
}

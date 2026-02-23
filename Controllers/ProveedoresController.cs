using ApiProveedores.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/proveedores")]
    public class ProveedoresController : ControllerBase
    {
        private readonly ProveedoresService _service;

        public ProveedoresController(ProveedoresService service)
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
        [HttpGet]
        public async Task<IActionResult> GetProveedorByClave([FromQuery] string? claveProveedor)
        {
            var resultado = await _service.RecuperaProveedorAsync(claveProveedor);
            return Ok(resultado);
        }
    }
}

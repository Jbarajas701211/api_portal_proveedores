using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiProveedores.Services;
using System.Threading.Tasks;
using System;
using ApiProveedores.Services.Exceptions;
using ApiProveedores.Dto;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Route("api/catalogos/tiendas")]
    [Authorize]
    public class TiendasController : ControllerBase
    {
        private readonly TiendaService _service;

        public TiendasController(TiendaService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ConsultarTodos(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 10,
            [FromQuery] string? filtro = null)
        {
            var resultado = await _service.BuscarTiendasPaginadoAsync(filtro, pagina, tamanioPagina);
            return Ok(resultado);
        }

    }
}

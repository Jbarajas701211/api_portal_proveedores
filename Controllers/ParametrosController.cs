using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiProveedores.Services;
using System.Threading.Tasks;
using ApiProveedores.Dto.Entrada;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Route("api/catalogos/parametros_sistema")]
    [Authorize]
    public class ParametrosController : ControllerBase
    {
        private readonly ParametroSistemaService _service;

        public ParametrosController(ParametroSistemaService service)
        {
            _service = service;
        }


        [Authorize(Roles = "LOGISTICA")]
        [HttpPost]
        public async Task<IActionResult> RegistrarParametro([FromBody] ParametroSistemaDto dto)
        {
            await _service.RegistrarParametroAsync(dto);
            return Ok(new { message = "Parámetro registrado correctamente." });
        }


        [Authorize(Roles = "LOGISTICA")]
        [HttpDelete("{clave}")]
        public async Task<IActionResult> EliminarParametro(string clave)
        {
            await _service.EliminarParametroAsync(clave);
            return Ok(new { message = "Parámetro eliminado correctamente." });
        }



        [Authorize(Roles = "LOGISTICA")]
        [HttpGet]
        public async Task<IActionResult> ConsultarTodos(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 10,
            [FromQuery] string? filtro = null)
        {
            var resultado = await _service.BuscarParametrosPaginadoAsync(filtro, pagina, tamanioPagina);
            return Ok(resultado);
        }

        [Authorize(Roles = "LOGISTICA")]
        [HttpPatch]
        public async Task<IActionResult> ActualizarValorParametro([FromBody] ActualizarValorParametroDto dto)
        {
            await _service.ActualizarValorParametroAsync(dto.Clave, dto.Valor);
            return Ok(new { message = "Valor actualizado correctamente." });
        }


    }
}

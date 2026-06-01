using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiProveedores.Services;
using System.Threading.Tasks;
using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;
using ApiProveedores.Services.Exceptions;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Route("api/catalogos/avisos")]
    [Authorize]
    public class AvisosController : ControllerBase
    {
        private readonly AvisosService _service;
        private readonly ILogger<AvisosService> _logger;

        public AvisosController(AvisosService service, ILogger<AvisosService> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarAviso([FromBody] AvisoDto dto)
        {
            try
            {
                await _service.RegistrarAvisoAsync(dto);
                return Ok(new { message = "Aviso registrado correctamente." });
            }
            catch (Exception ex)
            {
                // Aquí puedes agregar logging para ver el error real
                _logger.LogError(ex, "Error al registrar aviso");
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarAviso(int id)
        {
            await _service.EliminarAvisoAsync(id);
            return Ok(new { message = "Aviso eliminado correctamente." });
        }


        [HttpGet]
        public async Task<IActionResult> ConsultarTodos(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 10,
            [FromQuery] string? filtro = null)
        {
            var resultado = await _service.BuscarAvisosPaginadoAsync(filtro, pagina, tamanioPagina);
            return Ok(resultado);
        }


        [HttpPatch]
        public async Task<IActionResult> ActualizarAvisoAsync([FromBody] AvisoDto avisoDto)
        {
            try
            {
                await _service.ActualizarAvisoAsync(avisoDto);
                return Ok(new { message = "Aviso actualizado correctamente." });
            }
            catch (Exception ex)
            {
                throw new AvisoException(ex.Message);
            }
           
        }


    }
}

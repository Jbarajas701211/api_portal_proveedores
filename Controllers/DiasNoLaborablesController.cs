using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiProveedores.Services;
using System.Threading.Tasks;
using System;
using ApiProveedores.Services.Exceptions;
using ApiProveedores.Dto.Entrada;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Route("api/catalogos/dias_no_laborables")]
    [Authorize]
    public class DiasNoLaborablesController : ControllerBase
    {
        private readonly DiaNoLaborableService _service;

        public DiasNoLaborablesController(DiaNoLaborableService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ConsultarTodos(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 10,
            [FromQuery] int? anio = null)
        {
            var resultado = await _service.ConsultarTodosAsync(pagina, tamanioPagina, anio);
            return Ok(resultado);
        }

        [Authorize(Roles = "LOGISTICA")]
        [HttpPost]
        public async Task<IActionResult> RegistrarDiaNoLaborable([FromBody] DiaNoLaborableDto dto)
        {
            if (dto == null)
                return BadRequest(new { mensaje = "Datos inválidos." });

            await _service.RegistrarDiaNoLaborableAsync(dto.Fecha, dto.Descripcion);
            return Ok(new { mensaje = "Día no laborable registrado correctamente." });
        }

        [Authorize(Roles = "LOGISTICA")]
        [HttpPatch]
        public async Task<IActionResult> ActualizaDescripcionDiaNoLaborable([FromBody] DiaNoLaborableDto dto)
        {
            if (dto == null)
                return BadRequest(new { mensaje = "Datos inválidos." });
            await _service.ActulizaDiaNoLaborableAsync(dto.Fecha, dto.Descripcion);
            return Ok(new { mensaje = "Descripción del día no laborable actualizada correctamente." });
        }

        [Authorize(Roles = "LOGISTICA")]
        [HttpDelete]
        public async Task<IActionResult> EliminaDiaNoLaborable([FromQuery] DateTime dia)
        {
            if (dia == default)
                return BadRequest(new { mensaje = "Fecha inválida." });
            await _service.EliminarAsync(dia);
            return Ok(new { mensaje = "Día no laborable eliminado correctamente." });
        }

    }
}

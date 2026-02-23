using ApiProveedores.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using ApiProveedores.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using ApiProveedores.Dto.Http;
using ApiProveedores.Dto.Entrada;
using ApiProveedores.Helper;

namespace ApiProveedores.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/capacidad")]
    public class CapacidadController : ControllerBase
    {
        private readonly CapacidadService _service;

        public CapacidadController(CapacidadService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("resumen")]
        public async Task<ActionResult<ResumenCapacidadDto>> ObtenerResumen(
            [FromQuery] string? centroDistribucion,
            [FromQuery] string? origen,
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin)
        {

            if ((fechaInicio.HasValue && !fechaFin.HasValue) ||
                (!fechaInicio.HasValue && fechaFin.HasValue))
            {
                throw new ResumenCapacidadesException("Debe especificar tanto 'fechaInicio' como 'fechaFin' para filtrar por rango de fechas.");
            }

            var resultado = await _service.ObtenerResumenCapacidadAsync(
                centroDistribucion,
                origen,
                fechaInicio,
                fechaFin
            );

            return Ok(resultado);
        }

        [Authorize]
        [HttpGet("semanal")]
        public async Task<ActionResult<CapacidadDiaDto>> ObtenerSemanal(
            [FromQuery] string? centroDistribucion)
        {

            var resultado = await _service.ObtenerCapaciadPorDiaAsync(
                centroDistribucion
            );

            return Ok(resultado);
        }


        [Authorize(Roles = "LOGISTICA")]
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarCapacidadDeUso([FromBody] RegistrarCapacidadDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (usuarioId, _) = User.RequireIds();

            var Fecha = dto.Fecha.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dto.Fecha, DateTimeKind.Utc)
                : dto.Fecha.ToUniversalTime();

            await _service.RegistrarCapacidadUsoAsync(
                dto.Cd,
                dto.Origen,
                Fecha,
                dto.Cantidad,
                dto.Cantidad > 0 ? "ASIGNACION" : "LIBERACION",
                ((int) usuarioId)
            );

            return Ok(new { message = "Capacidad registrada correctamente." });
        }

        [Authorize]
        [HttpPost("registrar_lote")]
        public async Task<IActionResult> RegistrarUsoLote([FromBody] List<RegistrarCapacidadDto> lista)
        {
            if (lista == null || lista.Count == 0)
                return BadRequest(new { mensaje = "El arreglo de capacidades está vacío." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = User;
            var userId = user.FindFirst("sub")?.Value
              ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var usuarioId))
                return Unauthorized(new { mensaje = "Usuario no autenticado correctamente." });

            foreach (var dto in lista)
            {
                var fecha = dto.Fecha.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dto.Fecha, DateTimeKind.Utc)
                    : dto.Fecha.ToUniversalTime();

                await _service.RegistrarCapacidadUsoAsync(
                    dto.Cd,
                    dto.Origen,
                    fecha,
                    dto.Cantidad,
                    dto.Cantidad > 0 ? "ASIGNACION" : "LIBERACION",
                    ((int)usuarioId)
                );
            }

            return Ok(new { message = "Capacidades registradas correctamente." });
        }

    }
}

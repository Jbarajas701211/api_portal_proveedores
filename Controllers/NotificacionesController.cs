using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiProveedores.Services;
using System.Threading.Tasks;
using System;
using ApiProveedores.Helper;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Linq;
using System.Security.Cryptography;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Route("api/notificaciones")]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly NotificacionesService _service;

        public NotificacionesController(NotificacionesService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("mias")]
        public async Task<IActionResult> ObtenerMias()
        {
            try {
                var (usuarioId, _) = User.RequireIds();
                var notificaciones = await _service.ObtenerUltimasNotificacionesPorUsuarioAsync(usuarioId);
                return Ok(notificaciones);
            } catch (Exception ex) {
                return StatusCode(500, new { mensaje = "Error interno al obtener las notificaciones.", detalle = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("todas_mias")]
        public async Task<IActionResult> ObtenerTodasMias(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamano = 15,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null)
        {

            if (pagina < 1) pagina = 1;
            if (tamano <= 0) tamano = 15;

            if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde.Value == fechaHasta.Value)
            {
                fechaHasta = fechaHasta.Value.AddDays(1);
            }

            if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde >= fechaHasta)
                return BadRequest(new ProblemDetails
                {
                    Title = "Parámetros inválidos",
                    Detail = "'fechaDesde' debe ser menor que 'fechaHasta' (el rango es [desde, hasta))."
                });

            var (usuarioId, _) = User.RequireIds();

            var resultado = await _service.ObtenerNotificacionesPorUsuarioAsync(
                usuarioId: usuarioId,
                pagina: pagina,
                tamano: tamano,
                fechaDesde: fechaDesde,
                fechaHasta: fechaHasta
            );

            return Ok(resultado);
        }

        [Authorize]
        [HttpPost("{notificacionId:long}/leer")]
        public async Task<IActionResult> MarcarLeida(
            [FromRoute] long notificacionId,
            CancellationToken ct)
        {
            var (usuarioId, _) = User.RequireIds();
            await _service.MarcarLeidaAsync(notificacionId, usuarioId, ct);
            return Ok();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] DateTime? since,
                                             [FromQuery] int take = 15,
                                             CancellationToken ct = default)
        {

            var (usuarioId, _) = User.RequireIds();


            // (1) Firma barata - construir ETag
            var (maxUpd, total) = await _service.ObtenerFirmaUsuarioAsync(usuarioId, ct);
            var etag = maxUpd is null ? "W/\"empty\"" : $"W/\"{maxUpd.Value.ToUniversalTime().Ticks}-{total}\"";

            // (2) Validacion condicional
            var ifNoneMatch = Request.Headers.IfNoneMatch.ToString();
            if (!string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch, etag, StringComparison.Ordinal))
            {
                Response.Headers.ETag = etag;
                if (maxUpd is not null) Response.Headers.LastModified = maxUpd.Value.ToUniversalTime().ToString("R");
                Response.Headers.CacheControl = "private, no-cache, must-revalidate";
                return StatusCode(StatusCodes.Status304NotModified);
            }

            // (3) Traer pagina
            var items = await _service.ObtenerNotificacionesUsuarioAsync(usuarioId, since, take, ct);

            Response.Headers.ETag = etag;
            if (maxUpd is not null) Response.Headers.LastModified = maxUpd.Value.ToUniversalTime().ToString("R");
            Response.Headers.CacheControl = "private, no-cache, must-revalidate";

            // Sugerencia: regresa also el cursor (último UpdatedAt) si lo necesitas
            var nextCursor = items.Count > 0
                ? (DateTime?)(items.Last().LeidaEn ?? items.Last().Fecha)
                : null;

            return Ok(new { total = items.Count, items, nextCursor });
        }

    }
}

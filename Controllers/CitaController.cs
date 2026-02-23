using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ApiProveedores.Services.Citas;
using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Mappers;
using ApiProveedores.Helper;
using ApiProveedores.Services.Exceptions;
using System.Threading;
using System;
using System.Security.Claims;
using ApiProveedores.Models;

namespace ApiProveedores.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/cita")]
    public class CitaController : ControllerBase
    {
        private readonly CitaService _citaService;
        private readonly DatosTransportistaService _datosTransportistaService;

        public CitaController(CitaService citaService, DatosTransportistaService datosTransportistaService)
        {
            _citaService = citaService;
            _datosTransportistaService = datosTransportistaService;
        }

        [Authorize(Roles = "LOGISTICA")]
        [HttpGet("{citaId:long}/autorizar")]
        public async Task<IActionResult> AutorizarCita([FromRoute] long citaId, [FromQuery] long proveedorId)
        {
            await _citaService.AutorizarDenegarSolicitudAsync(citaId, proveedorId, true);
            return Ok(new
            {
                message = "Cita autorizada",
            });
        }

        [Authorize(Roles = "LOGISTICA")]
        [HttpGet("{citaId:long}/denegar")]
        public async Task<IActionResult> DenegarCita([FromRoute] long citaId, [FromQuery] long proveedorId)
        {
            await _citaService.AutorizarDenegarSolicitudAsync(citaId, proveedorId, false);
            return Ok(new
            {
                message = "Cita denegada",
            });
        }

        [Authorize(Roles = "PROVEEDOR")]
        [HttpPost]
        public async Task<IActionResult> RegistrarCita([FromBody] CrearCitaDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (userId, proveedorId) = User.RequireIds();

            var folioCita = await _citaService.RegistrarCitaAsync(dto, proveedorId, userId);
            return Ok(new
            {
                message = "Cita registrada.",
                folio = folioCita,
            });
        }


        [Authorize(Roles = "PROVEEDOR")]
        [HttpGet("{citaId:long}/solicita_autorizacion")]
        public async Task<IActionResult> SolicitaAutorizacion([FromRoute] long citaId)
        {
            var (_, proveedorId) = User.RequireIds();
            await _citaService.SolicitarAutorizacionAsync(citaId, proveedorId);

            return Ok(new
            {
                message = $"Se ha solicitado la autorizacion al equipo de logistica.",
            });
        }

        [Authorize(Roles = "PROVEEDOR,LOGISTICA,ONEST")]
        [HttpGet]
        public async Task<IActionResult> ConsultarCita([FromQuery] string uuid)
        {
            Cita resultado = null;
            var rol = User.FindFirst(ClaimTypes.Role)?.Value;
            if (rol == "PROVEEDOR")
            {
                var (_, proveedorId) = User.RequireIds();
                resultado = await _citaService.RecuperaCitaAsync(uuid, proveedorId);
            }
            else {
                resultado = await _citaService.RecuperaCitaAsync(uuid);
            }

            if (resultado == null)
            {
                throw new CitaException("Cita invalida.");
            }
            return Ok(CitaMapper.ToDto(resultado));
        }


        [Authorize(Roles = "PROVEEDOR")]
        [HttpDelete]
        public async Task<IActionResult> EliminarCita([FromQuery] long id)
        {
            var (_, proveedorId) = User.RequireIds();
            await _citaService.EliminarCitaAsync(id, proveedorId);

            return Ok();
        }


        [Authorize(Roles = "PROVEEDOR")]
        [HttpPatch("{citaId:long}/generar_folio")]
        public async Task<IActionResult> GenerarFolio([FromRoute] long citaId)
        {   
            var (usuarioId, proveedorId) = User.RequireIds();
            await _citaService.GenerarFolioAsync(citaId, proveedorId, usuarioId);
            return Ok(new
            {
                message = $"Se ha generado el folio para la cita.",
            });
        }

        [Authorize(Roles = "PROVEEDOR")]
        [HttpPatch("{citaId:long}/cancelar")]
        public async Task<IActionResult> CancelarCita([FromRoute] long citaId)
        {
            var (usuarioId, proveedorId) = User.RequireIds();
            await _citaService.CancelarAsync(citaId, usuarioId, proveedorId);
            return Ok(new
            {
                message = $"Se ha cancelado la cita.",
            });
        }

        [Authorize(Roles = "PROVEEDOR")]
        [HttpPatch("{citaId:long}/datos_transportista")]
        public async Task<IActionResult> ActualizarDatosTransportista(
            [FromRoute] long citaId,
            [FromBody] TransporteDto dto)
        {
            var (usuarioId, proveedorId) = User.RequireIds();

            await _datosTransportistaService.ActualizarDatosTransporteAsync(citaId, proveedorId, dto);

            return Ok(new
            {
                message = $"Datos del transportista actualizados.",
            });
        }

        [Authorize(Roles = "PROVEEDOR")]
        [HttpPatch("{citaId:long}")]
        public async Task<IActionResult> ActualizarDatosCita(
            [FromRoute] long citaId,
            [FromBody] ActualizaDatosCitaDto dto)
        {
            var (usuarioId, proveedorId) = User.RequireIds();

            await _citaService.ActualizarDatosCitaAsync(citaId, proveedorId, dto);

            return Ok(new
            {
                message = $"Datos del cita actualizados.",
            });
        }

        [Authorize(Roles = "PROVEEDOR")]
        [HttpGet("lotes")]
        public async Task<IActionResult> RecuperarLotesAsync()
        {
            var (_, proveedorId) = User.RequireIds();
            var resultado = await _citaService.ObtenerLotesUltimasDosSemanasHastaMananaAsync(proveedorId);
            return Ok(resultado);
        }


        [Authorize(Roles = "PROVEEDOR")]
        [HttpGet("por_lote")]
        public async Task<IActionResult> RecuperarLotesAsync([FromQuery] string lote)
        {
            var (_, proveedorId) = User.RequireIds();
            var resultado = await _citaService.RecuperaCitasPorLoteAsync(lote, proveedorId);
            return Ok(resultado);
        }


        [Authorize(Roles = "ONEST,LOGISTICA,PROVEEDOR")]
        [HttpGet("consultar")]
        public async Task<IActionResult> GetCitas(
            [FromQuery] string? numeroOrden,
            [FromQuery] long? proveedorId,
            [FromQuery] string? numeroLote,
            [FromQuery] string? folio,
            [FromQuery] long? idCita,
            [FromQuery] DateTime? fechaInicio,
            [FromQuery] DateTime? fechaFin,
            [FromQuery] bool soloHoy = false,
            [FromQuery] TipoConsultaCita tipo = TipoConsultaCita.Todas,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 50,
            CancellationToken ct = default)
        {
            if (!soloHoy && fechaInicio.HasValue && fechaFin.HasValue && fechaFin < fechaInicio)
                return BadRequest("El rango de fechas es inválido.");

            var filtros = new CitasFiltroDto
            {
                NumeroOrden = numeroOrden,
                ProveedorId = proveedorId,
                NumeroLote = numeroLote,
                Folio = folio,
                FechaInicio = soloHoy ? null : fechaInicio,
                FechaFin = soloHoy ? null : fechaFin,
                SoloHoy = soloHoy,
                Tipo = tipo,
                IdCita = idCita,
            };

            var rol = User.FindFirst(ClaimTypes.Role)?.Value;
            if (rol == "PROVEEDOR")
            {
                var (_, proveedorIdLoged) = User.RequireIds();
                filtros.ProveedorId = proveedorIdLoged;
            }

            var result = await _citaService.FiltrarAsync(filtros, pagina, tamanioPagina, ct);
            return Ok(result);
        }
    }
}

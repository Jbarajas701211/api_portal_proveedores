using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ApiProveedores.Services.Citas;
using ApiProveedores.Dto.Entrada;
using ApiProveedores.Helper;
using ApiProveedores.Dto.Mappers;

namespace ApiProveedores.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/detalle_cita")]
    public class DetalleCitaController : ControllerBase
    {
        private readonly CitaService _citaService;
        private readonly DetalleCitaService _detalleCitaservice;

        public DetalleCitaController(CitaService citaService, DetalleCitaService detalleCitaservice)
        {
            _citaService = citaService;
            _detalleCitaservice = detalleCitaservice;
        }

        [Authorize(Roles = "PROVEEDOR")]
        [HttpPost]
        public async Task<IActionResult> RegistrarDetalleCita([FromBody] CrearCitaDetalleDto dto)
        {
            var (_, proveedorId) = User.RequireIds();

            var item = await _detalleCitaservice.RegistrarDetalleCitaAsync(dto, proveedorId);
            return Ok(new
            {
                message = "Detalle cita registrado.",
                detalle = item.ToDto(),
            });
        }

        [Authorize(Roles = "PROVEEDOR,LOGISTICA,ONEST")]
        [HttpGet("{citaId:long}")]
        public async Task<IActionResult> RecuperarDetallesCta(long citaId)
        {

            var rol = User.FindFirst(ClaimTypes.Role)?.Value;
            if (rol == "PROVEEDOR")
            {
                var (_, proveedorId) = User.RequireIds();
                return Ok(new
                {
                    detalles = await _detalleCitaservice.ObtenerDetallesPorCitaAsync(citaId, proveedorId)
                });
            }
            else
            {
                return Ok(new
                {
                    detalles = await _detalleCitaservice.ObtenerDetallesPorCitaAsync(citaId)
                });
            }
        }

        [Authorize(Roles = "PROVEEDOR")]
        [HttpPatch]
        public async Task<IActionResult> ActualizarDetalleCita([FromBody] ActualizarCitaDetalleDto dto)
        {
            var (_, proveedorId) = User.RequireIds();

            var item = await _detalleCitaservice.ActualizarDetalleCitaAsync(dto, proveedorId);
            return Ok(new
            {
                mensaje = "Detalle cita actualizado.",
                detalle = item.ToDto(),
            });
        }

        [Authorize(Roles = "PROVEEDOR")]
        [HttpDelete]
        public async Task<IActionResult> EliminarDetalleCita([FromBody] EliminaCitaDetalleDto dto)
        {
            var (_, proveedorId) = User.RequireIds();

            await _detalleCitaservice.EliminarDetalleCitaAsync(dto, proveedorId);
            return Ok(new
            {
                mensaje = "Detalle cita eliminado"
            });
        }
    }
}

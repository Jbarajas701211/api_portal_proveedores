using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ApiProveedores.Services.Citas;
using ApiProveedores.Dto.Entrada;
using ApiProveedores.Helper;

namespace ApiProveedores.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/entrega")]
    public class EntregasController : ControllerBase
    {
        private readonly EntregaService _service;
        public EntregasController(EntregaService service)
        {
            _service = service;
        }

        [Authorize(Roles = "ONEST")]
        [HttpPost]
        public async Task<IActionResult> RegistrarEntrega(EntregaDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (userId, _) = User.RequireIds();
            var entrega = await _service.RegistraEntregaAsync(dto, userId);

            return Ok(new {
                mensaje = "Entrega registrada correctamente.",
                Entrega = entrega
            });
        }

        [Authorize(Roles = "ONEST")]
        [HttpPost("falla_masiva")]
        public async Task<IActionResult> RegistraFallaMasiva(FallaMasivaDto dto)
        {

            var (userId, _) = User.RequireIds();
            await _service.RegistraFallaMasivaAsync(dto.CitaIds, dto.Notas, userId);

            return Ok(new
            {
                mensaje = "Falla masiva registrada correctamente.",
            });
        }

        [Authorize(Roles = "ONEST, LOGISTICA, PROVEEDOR")]
        [HttpGet]
        public async Task<IActionResult> RecuperaEntregas([FromQuery] long idCita)
        {

            var result = await _service.ObtenerEntregasPorCitaAsync(idCita);

            return Ok(result);
        }

    }
}

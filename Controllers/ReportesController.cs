using ApiProveedores.Dto.Entrada;
using ApiProveedores.Helper;
using ApiProveedores.Models.Enum;
using ApiProveedores.Services.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/reportes")]
    public class ReportesController : ControllerBase
    {
        private readonly ReporteService _reporteService;

        public ReportesController(ReporteService reporteService)
        {
            _reporteService = reporteService;
        }

        [HttpPost]
        public async Task<IActionResult> GenerarReporte([FromBody] CriterioReporte criterioReporte)
        {
            try
            {
                var (usuarioId, _) = User.RequireIds();
                var respuesta = await _reporteService.GenerarReporteAsync(criterioReporte, usuarioId); 
                return Ok(new { mensaje = "Reporte en proceso de generación." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al generar el reporte.", detalle = ex.Message });
            }
        }
    }
}

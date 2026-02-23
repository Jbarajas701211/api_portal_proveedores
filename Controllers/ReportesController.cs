using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiProveedores.Services;
using System.Threading.Tasks;
using System;
using ApiProveedores.Services.Exceptions;
using ApiProveedores.Dto;
using ApiProveedores.Services.Reportes;
using System.Collections.Generic;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Route("api/reportes")]
    [Authorize]
    public class ReportesController : ControllerBase
    {
        private readonly ReporteResumenOrdenesService _reporteResumenOrdenes;
        private readonly ReporteDetalleOrdenService _reporteDetalleOrden;
        public ReportesController(
            ReporteResumenOrdenesService _resumenService, 
            ReporteDetalleOrdenService reporteDetalleOrden)
        {
            _reporteResumenOrdenes = _resumenService;
            _reporteDetalleOrden = reporteDetalleOrden;
        }


        [Authorize]
        [HttpPost("ordenes_resumen")]
        public async Task<IActionResult> GenerarResumenOrdenes([FromBody] Dictionary<string, object> filtros)
        {
            await _reporteResumenOrdenes.GenerarReporteAsync(filtros, User);
            return Ok();
        }

        [Authorize]
        [HttpPost("detalle_orden")]
        public async Task<IActionResult> GenerarResumenDetalleOrden([FromBody] Dictionary<string, object> filtros)
        {
            await _reporteDetalleOrden.GenerarReporteAsync(filtros, User);
            return Ok();
        }
    }
}

using ApiProveedores.Models.Factura;
using ApiProveedores.Services;
using ApiProveedores.Services.PubSub;
using ApiProveedores.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApiProveedores.Models.Enum;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(StorageService storageService, DashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpGet("carga_dashboard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardInformationAsync(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanioPagina = 10,
            [FromQuery] DateTime? fechaInicial = null,
            [FromQuery] DateTime? fechaFinal = null,
            [FromQuery] int? idUsuario = null
            )
        {
            try
            {
                var resultado = await _dashboardService.ObtenerInformacionDashboardAsync(idUsuario, fechaInicial, fechaFinal);
                return Ok(resultado);

            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado en el proceso de obtener información del dashboard: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error inesperado en el proceso de obtener información del dashboard.");
            }
        }
    }
}

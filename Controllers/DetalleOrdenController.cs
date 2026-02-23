using ApiProveedores.Services;
using ApiProveedores.Dto;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using ApiProveedores.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ApiProveedores.Models;
using ApiProveedores.Helper;

namespace ApiProveedores.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/detalle_orden")]
    public class DetalleOrdenController : ControllerBase
    {
        private readonly DetalleOrdenService _service;
        

        public DetalleOrdenController(DetalleOrdenService service)
        {
            _service = service;

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ObtenerDetalleOrdene([FromQuery] FiltroDetalleOrdenDto filtro)
        {
            var rol = User.FindFirst(ClaimTypes.Role)?.Value;
            if (rol == "PROVEEDOR")
            {
                var (_, proveedorId) = User.RequireIds();
                filtro.Proveedor = proveedorId.ToString();
            }

            var resultado = await _service.ConsultaDetalleOrdenesAsync(filtro);
            return Ok(resultado);

        }

    }
}

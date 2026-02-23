using ApiProveedores.Services;
using ApiProveedores.Dto;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ApiProveedores.Helper;

namespace ApiProveedores.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/orden")]
    public class OrdenController : ControllerBase
    {
        private readonly OrdenService _service;
        

        public OrdenController(OrdenService service, ProveedoresService proveedorService)
        {
            _service = service;

        }

        [Authorize(Roles = "PROVEEDOR")]
        [HttpGet("recupera_orden")]
        public async Task<IActionResult> RecuperaOrden([FromQuery] string noOrden)
        {
            var (_, proveedorId) = User.RequireIds();
            var orden = await _service.RecuperaOrdenAsync(noOrden, proveedorId);
            return Ok(orden);
        }

        [Authorize]
        [HttpGet("consulta")]
        public async Task<IActionResult> ObtenerOrdenes([FromQuery] FiltroOrdenDto filtro)
        {
            var rol = User.FindFirst(ClaimTypes.Role)?.Value;
            if (rol == "PROVEEDOR")
            {
                var (_, proveedorId) = User.RequireIds();
                filtro.Proveedor = proveedorId.ToString();
            }

            var resultado = await _service.ConsultarOrdenesAsync(filtro, rol);
            return Ok(resultado);

        }

    }
}

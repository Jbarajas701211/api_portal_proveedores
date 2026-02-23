using ApiProveedores.Dto.Salida;
using ApiProveedores.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProveedores.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/centros_distribucion")]
    public class CentrosDistribucionController : ControllerBase
    {
        private readonly CentroDistribucionService _service;

        public CentrosDistribucionController(CentroDistribucionService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetCentrosDistribucion(CancellationToken ct)
        {
            var items = await _service.ObtenerTodosAsync(ct);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> PostCentroDistribución(CentroDistribucionDto centroDistribucionDto) 
        { 
            var response = await _service.CrearCentroDistribucionAsync(centroDistribucionDto);
            return Ok(response);
        }

        [HttpPatch]
        public async Task<IActionResult> PatchCentroDistribucion(CentroDistribucionDto centroDistribucionDto)
        {
            var response = await _service.ActualizarCentroDistribucionAsync(centroDistribucionDto);
            return Ok(response);
        }


    }
}

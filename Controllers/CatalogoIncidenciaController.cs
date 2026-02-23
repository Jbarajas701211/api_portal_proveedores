using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ApiProveedores.Services.Citas;
using ApiProveedores.Dto.Entrada;
using Google.Api;
using ApiProveedores.Dto;

namespace ApiProveedores.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/catalogo/incidencias")]
    public class CatalogoIncidenciaController : ControllerBase
    {
        private readonly IncidenciaService _service;
        public CatalogoIncidenciaController(IncidenciaService service)
        {
            _service = service;
        }

        [Authorize(Roles = "LOGISTICA,ONEST")]
        [HttpGet]
        public async Task<IActionResult> GetCatalogoIncidencia()
        {
            var incidenciaItems = await _service.GetCatalogoIncidenciaAsync();
            return Ok(incidenciaItems);
        }

        [Authorize(Roles = "LOGISTICA")]
        [HttpPost]
        public async Task<IActionResult> PostCatalogoIncidencia(IncidenciaRequestDto dto)
        {
            var newItem = await _service.CreateCatalogoIncidenciaAsync(dto);
            return Ok(new
            {
                mensaje = "Incidencia creada correctamente.",
                incidencia = newItem
            });
        }

        [Authorize(Roles = "LOGISTICA")]
        [HttpPatch]
        public async Task<IActionResult> PatchCatalogoIncidencia(IncidenciaRequestDto dto )
        {
            var updatedItem = await _service.UpdateCatalogoIncidenciaAsync(dto);
            return Ok(new
            {
                mensaje = "Incidencia actualizada correctamente.",
                incidencia = updatedItem
            });
        }
    }
}

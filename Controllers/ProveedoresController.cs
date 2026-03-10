using ApiProveedores.Services;
using ApiProveedores.Dto.Entrada;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/proveedores")]
    public class ProveedoresController : ControllerBase
    {
        private readonly ProveedoresService _service;

        public ProveedoresController(ProveedoresService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar(
            [FromQuery] string? filtro,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanio = 10)
        {
            var resultado = await _service.BuscarProveedoresPaginadoAsync(filtro, pagina, tamanio);
            return Ok(resultado);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetProveedorByClave([FromQuery] string? claveProveedor)
        {
            var resultado = await _service.RecuperaProveedorAsync(claveProveedor);
            return Ok(resultado);
        }

        [Authorize]
        [HttpGet("documentos")]
        public async Task<IActionResult> GetDocumentosByProveedor([FromQuery] long idProveedor)
        {
            var resultado = await _service.ObtenerDocumentosPorProveedorAsync(idProveedor);
            return Ok(resultado);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateProveedor([FromBody] ProveedorDto proveedorDto)
        {
            if (proveedorDto == null)
                return BadRequest(new { mensaje = "Datos inválidos." });

            try
            {
                var actualizado = await _service.ActualizarProveedorAsync(proveedorDto);

                if (actualizado)
                    return Ok(new { mensaje = "Proveedor actualizado correctamente." });

                return BadRequest(new { mensaje = "No se pudo actualizar el registro." });
            }
            catch (ApiProveedores.Services.Exceptions.ApiProveedoresException appEx)
            {
                return BadRequest(new { mensaje = appEx.Message ?? "No se pudo actualizar el registro." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "No se pudo actualizar el registro." });
            }
        }
    }
}

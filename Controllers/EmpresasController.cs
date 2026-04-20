using ApiProveedores.Dto;
using ApiProveedores.Dto.Catalogos;
using ApiProveedores.Dto.Entrada;
using ApiProveedores.Services;
using ApiProveedores.Services.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/catalogos/empresas")]
    public class EmpresasController : ControllerBase
    {
        private readonly EmpresaService _service;

        public EmpresasController(EmpresaService service)
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
            var resultado = await _service.BuscarEmpresasPaginadoAsync(filtro, pagina, tamanio);
            return Ok(resultado);
        }

        [HttpPost]
        public async Task<IActionResult> CrearEmpresa([FromBody] EmpresaDto dto)
        {
            try
            {
                var empresa = await _service.CrearEmpresaAsync(dto);
                return CreatedAtAction(nameof(Buscar), new { id = empresa.IdEmpresa }, empresa);
            }
            catch (ApiProveedoresException appEx)
            {
                return BadRequest(appEx.Message);
            }
           
        }

        [HttpPatch]
        public async Task<IActionResult> ActualizarEmpresa([FromBody] EmpresaDto dto)
        {
            try
            {
                var empresa = await _service.ActualizarEmpresaAsync(dto);
                if (empresa == null) return NotFound();
                return Ok(empresa);
            }
            catch (ApiProveedoresException appEx)
            {
                return BadRequest(appEx.Message);
            }

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarEmpresa(int id)
        {
            try
            {
                var eliminado = await _service.EliminarEmpresaAsync(id);
                if (!eliminado) return NotFound();
                return NoContent();
            }
            catch (ApiProveedoresException appEx)
            {
                return BadRequest(appEx.Message);
            }

        }
    }
}

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

        [Authorize]
        [HttpGet("documentos")]
        public async Task<IActionResult> GetDocumentosByProveedor([FromQuery] long idProveedor)
        {
            var resultado = await _service.ObtenerDocumentosPorProveedorAsync(idProveedor);
            return Ok(resultado);
        }

        [Authorize]
        [HttpPost("documentos")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddDocumentoProveedor([FromBody] List<ProveedorDocumentoDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
                return BadRequest(new { mensaje = "Datos inválidos." });

            try
            {
                var creado = await _service.AgregarDocumentosProveedorAsync(dtos);
                if (creado)
                    return Ok(new { mensaje = "Documento(s) asociado(s) al proveedor correctamente." });

                return BadRequest(new { mensaje = "No se pudo asociar los documentos al proveedor." });
            }
            catch (ApiProveedoresException appEx)
            {
                return BadRequest(new { mensaje = appEx.Message ?? "No se pudo asociar los documentos al proveedor." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "No se pudo asociar los documentos al proveedor." });
            }
        }

        [Authorize]
        [HttpDelete("documentos")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDocumentoProveedor([FromQuery] long idProveedor, [FromQuery] int documentoId)
        {
            try
            {
                var eliminado = await _service.EliminarDocumentoProveedorAsync(idProveedor, documentoId);
                if (eliminado)
                    return Ok(new { mensaje = "Documento desasociado del proveedor correctamente." });

                return BadRequest(new { mensaje = "No se pudo desasociar el documento del proveedor." });
            }
            catch (ApiProveedoresException appEx)
            {
                return BadRequest(new { mensaje = appEx.Message ?? "No se pudo desasociar el documento del proveedor." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "No se pudo desasociar el documento del proveedor." });
            }
        }

        [Authorize]
        [HttpPut("documentos")]
        public async Task<IActionResult> UpdateDocumentoProveedor([FromBody] List<ProveedorDocumentoDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
                return BadRequest(new { mensaje = "Datos inválidos." });

            try
            {
                var actualizado = await _service.ActualizarDocumentosProveedorAsync(dtos);
                if (actualizado)
                    return Ok(new { mensaje = "Documento(s) del proveedor actualizado(s) correctamente." });

                return BadRequest(new { mensaje = "No se pudo actualizar los documentos del proveedor." });
            }
            catch (ApiProveedores.Services.Exceptions.ApiProveedoresException appEx)
            {
                return BadRequest(new { mensaje = appEx.Message ?? "No se pudo actualizar los documentos del proveedor." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { mensaje = "No se pudo actualizar los documentos del proveedor." });
            }
        }
    }
}

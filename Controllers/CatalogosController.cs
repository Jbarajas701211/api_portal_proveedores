using ApiProveedores.Services;
using ApiProveedores.Dto.Entrada;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ApiProveedores.Dto.Catalogos;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace ApiProveedores.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/catalogos")]
    public class CatalogosController : ControllerBase
    {
        private readonly CatalogoService _service;

        public CatalogosController(CatalogoService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("documentos")]
        [ProducesResponseType(typeof(List<DocumentoTipoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDocumentosByProveedor()
        {
            var resultado = await _service.RecuperaTipoDocumento();
            return Ok(resultado);
        }

       
    }
}

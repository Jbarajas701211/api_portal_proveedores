using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ApiProveedores.Services.Citas;
using ApiProveedores.Dto;
using System;
using System.Security.Cryptography;
using System.Text;

namespace ApiProveedores.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/incidencia")]
    public class IncidenciaController : ControllerBase
    {
        private readonly IncidenciaService _service;
        public IncidenciaController(IncidenciaService service)
        {
            _service = service;
        }

        [Authorize(Roles = "ONEST")]
        [HttpPost]
        public async Task<IActionResult> RegistraIncidencia(IncidenciaDto dto)
        {
            var item = await _service.RegistraIncidenciaAsync(dto);
            return Ok(new {
                mensaje = "Incidencia registrada correctamente.",
                incidencia = item
            });
        }

        [Authorize(Roles = "ONEST")]
        [HttpPost("masiva")]
        public async Task<IActionResult> RegistraIncidenciaMasiva(IncidenciaMasivaDto dto)
        {
            var response = await _service.RegistraIncidenciaMasivaAsync(dto);
            return Ok(new
            {
                mensaje = "Incidencia masiva registrada correctamente.",
                incidencias = response.IncidenciasIds,
                masiva_hash = response.HashMasivo
            });
        }



        [Authorize(Roles = "ONEST")]
        [HttpPost("url_evidencias")]
        public async Task<IActionResult> SolicitaUrlConSessionParaUpload(SolicitaUrlSessionEvidenciasDto dto)
        {
            var item = await _service.SolicitaUrlConSessionParaUpload(dto);
            return Ok(new
            {
                urlSession = item
            });
        }


        [Authorize(Roles = "ONEST")]
        [HttpPost("url_evidencias/masiva")]
        public async Task<IActionResult> SolicitaUrlConSessionParaUploadMasiva(SolicitaUrlSessionEvidenciasMasivaDto dto)
        {
            var item = await _service.SolicitaUrlConSessionParaUploadMasivo(dto);
            return Ok(new
            {
                urlSession = item
            });
        }


        [Authorize(Roles = "ONEST")]
        [HttpPost("archivo_cargado")]
        public async Task<IActionResult> ArchivoCargado(MarcaArchivoCargadoDto dto)
        {
            await _service.MarcaArchivoCargado(dto);
            return Ok();
        }


        [Authorize(Roles = "ONEST,LOGISTICA,PROVEEDOR")]
        [HttpPost("url_descarga")]
        public async Task<IActionResult> UrlPrefirmadaParaDescarga(UrlDescargaEvidenciaDto dto)
        {
            var urlDescarga = await _service.SolicitaUrlPrefirmadaParaDescarga(dto);
            return Ok(new { url = urlDescarga });
        }

        [Authorize(Roles = "ONEST,LOGISTICA,PROVEEDOR")]
        [HttpGet("{citaId:long}")]
        public async Task<IActionResult> RecuperaIncidenciasPorCita([FromRoute] long citaId)
        {
            var incidenciaItems = await _service.ObtenerIncidenciasPorCitaAsync(citaId);
            return Ok(new { incidencias = incidenciaItems });
        }
    }
}

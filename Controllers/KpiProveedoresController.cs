namespace ApiProveedores.Controllers
{
    using ApiProveedores.Dto.Entrada;
    using ApiProveedores.Models;
    using ApiProveedores.Services;
    using Microsoft.AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;
    using ApiProveedores.Services.Exceptions;
    using ApiProveedores.Helper;
    using ApiProveedores.Services.Citas;
    using System.Security.Claims;
    using System;

    [ApiController]
    [Route("api/kpi_proveedores")]
    public class KpiProveedoresController : ControllerBase
    {
        private readonly KpiProveedoresService _service;
        private readonly ProveedoresService _proveedoresService;

        public KpiProveedoresController(KpiProveedoresService service, 
            ProveedoresService proveedoresService)
        {
            _service = service;
            _proveedoresService = proveedoresService;
        }

        [HttpPost]
        public async Task<ActionResult<List<KpiProveedorResult>>> ObtenerKpis(
            [FromBody] KpiProveedoresRequest request,
            CancellationToken ct)
        {

            if (request == null)
                throw new KpiProveedorException("El cuerpo de la petición es obligatorio.");

            if (request.Hasta.Date < request.Desde.Date)
                throw new KpiProveedorException("La fecha 'hasta' no puede ser menor que 'desde'.");


            var rol = User.FindFirst(ClaimTypes.Role)?.Value;
            if (rol == "PROVEEDOR")
            {
                var cveProveedor = User.FindFirst("cveprov")?.Value;
                request.ClaveProveedor = cveProveedor;
            }

            var proveedor = await _proveedoresService.RecuperaProveedorAsync(request.ClaveProveedor);
            if (proveedor == null) {
                request.ClaveProveedor = null;
            }
            var resultados = await _service.ObtenerKpisAsync(
                request.Desde,
                request.Hasta,
                string.IsNullOrEmpty(request.ClaveProveedor) ? 0 : proveedor.Id_proveedor,
                ct);

            return Ok(resultados);
        }
    }

}

using ApiProveedores.Models;
using ApiProveedores.Services.Exceptions;
using ApiProveedores.Services.PubSub;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ApiProveedores.Services.Reportes
{
    public class ReporteDetalleOrdenService : BasePubSubService
    {
        private readonly OrdenService _ordenService;
        public ReporteDetalleOrdenService(GenericPubSubPublisher publisher, OrdenService ordenService) : base(publisher) {
            _ordenService = ordenService;
        }


        public async override Task GenerarReporteAsync(IDictionary<string, object> filtrosReporte, ClaimsPrincipal user)
        {
            string cveProveedor = string.Empty;
            string numeroOrden;
            object valor;

            var userId = user.FindFirst("sub")?.Value
                ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            filtrosReporte.Add("IdUsuario", userId);

            
            var rol = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (rol == "PROVEEDOR")
            {
                cveProveedor = user.FindFirst("cveprov")?.Value;
                filtrosReporte["ClaveProveedor"] = cveProveedor;
            }
            

            if (!filtrosReporte.TryGetValue("NumeroOrden", out valor) || valor == null)
            {
                throw new ReporteException("El filtro 'NumeroOrden' es obligatorio.");
            }
            numeroOrden  = valor.ToString();
            filtrosReporte["NumeroOrden"] = numeroOrden;

            Orden orden = null;
            if (rol == "PROVEEDOR") 
            {
                orden = await _ordenService.RecuperaOrdenAsync(numeroOrden, cveProveedor);
            }
            else
            {
                orden = await _ordenService.RecuperaOrdenAsync(numeroOrden);
            }
            
            if (orden == null) {
                throw new ReporteException("La orden es inválida.");
            } else
            {
                filtrosReporte["ClaveProveedor"] = orden.Cveprov;
            }

            var mensaje = new
            {
                tipo = "reporte_detalle_orden",
                filtros = filtrosReporte
            };
            await EnviarMensajeAsync("citas-reporting-data-topic", mensaje);
        }
    }

}

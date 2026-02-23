using ApiProveedores.Services.Exceptions;
using ApiProveedores.Services.Helper;
using ApiProveedores.Services.PubSub;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ApiProveedores.Services.Reportes
{
    public class ReporteResumenOrdenesService : BasePubSubService
    {
        public ReporteResumenOrdenesService(GenericPubSubPublisher publisher) : base(publisher) { }


        public async override Task GenerarReporteAsync(IDictionary<string, object> filtrosReporte, ClaimsPrincipal user)
        {

            var userId = user.FindFirst("sub")?.Value
                ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            filtrosReporte.Add("IdUsuario", userId);

            var rol = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;


            string cveProveedor;

            if (rol == "PROVEEDOR")
            {
                cveProveedor = user.FindFirst("cveprov")?.Value;
                filtrosReporte["ClaveProveedor"] = cveProveedor;
            }
            else
            {
                object valor;
                if (filtrosReporte.TryGetValue("ClaveProveedor", out valor) && valor != null) {
                    cveProveedor = valor.ToString();
                    filtrosReporte["ClaveProveedor"] = cveProveedor;
                }
            }

            // valida filtro por fecha pedido
            DateTime? fechaPedidoIni = HelperFechasReportes.TryGetDate(filtrosReporte, "FechaPedidoInicio");
            DateTime? fechaPedidoFin = HelperFechasReportes.TryGetDate(filtrosReporte, "FechaPedidoFin");

            if (fechaPedidoIni.HasValue ^ fechaPedidoFin.HasValue)
                throw new ReporteException("Si se especifica 'FechaPedidoInicio' o 'FechaPedidoFin', ambos son obligatorios.");

            if (fechaPedidoIni.HasValue && fechaPedidoFin.HasValue)
            {
                if (fechaPedidoFin.Value <= fechaPedidoIni.Value)
                    throw new ReporteException("'FechaPedidoFin' debe ser mayor que 'FechaPedidoInicio'.");

                var finNormalizado = fechaPedidoFin.Value.Date.AddDays(1).AddTicks(-1);

                filtrosReporte["FechaPedidoInicio"] = fechaPedidoIni.Value;
                filtrosReporte["FechaPedidoFin"] = finNormalizado;
            }


            // valida filtro por fecha vencimiento
            DateTime? fechaVencimientoIni = HelperFechasReportes.TryGetDate(filtrosReporte, "FechaVigenciaInicio");
            DateTime? fechaVencimientoFin = HelperFechasReportes.TryGetDate(filtrosReporte, "FechaVigenciaFin");

            if (fechaVencimientoIni.HasValue ^ fechaVencimientoFin.HasValue)
                throw new ReporteException("Si se especifica 'FechaVencimientoInicio' o 'FechaVencimientoFin', ambos son obligatorios.");

            if (fechaVencimientoIni.HasValue && fechaVencimientoFin.HasValue)
            {
                if (fechaVencimientoFin.Value <= fechaVencimientoIni.Value)
                    throw new ReporteException("'FechaVencimientoFin' debe ser mayor que 'FechaVencimientoInicio'.");

                var finNormalizado = fechaVencimientoFin.Value.Date.AddDays(1).AddTicks(-1);

                filtrosReporte["FechaVencimientoInicio"] = fechaVencimientoIni.Value;
                filtrosReporte["FechaVencimientoFin"] = finNormalizado;
            }


            var mensaje = new
            {
                tipo = "reporte_resumen_ordenes",
                filtros = filtrosReporte
            };
            await EnviarMensajeAsync("citas-reporting-data-topic", mensaje);
        }
    }

}

using ApiProveedores.Helper;
using ApiProveedores.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProveedores.Services.Citas
{
    public enum TipoOperacion
    {
        BLOQUEADA,
        DESBLOQUEDA,
    }
    public class HelperOrdenService
    {
        private readonly PortalDbContext _context;

        public HelperOrdenService(PortalDbContext context)
        {
            _context = context;
        }

        private async Task DesactivarSeguimientoOrdenAsync(
            string noOrden,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(noOrden))
                return;

            var oc = noOrden.Trim();

            await _context.OrdenesSeguimiento
                .Where(x => x.Nopedido == oc && x.EstadoActivo)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.EstadoActivo, _ => false), ct);
        }


        


        public async Task TraceOrdenesDeProcesamientoAsync(
            Cita cita,
            long usuarioId,
            string descripcion,
            TipoOperacion tipoOperacion = TipoOperacion.BLOQUEADA,
            CancellationToken ct = default)
        {
            var ordenes = cita.Detalles
                .Select(d => d.Oc)
                .Where(oc => !string.IsNullOrWhiteSpace(oc))
                .Select(oc => oc.Trim())
                .Distinct()
                .ToList();

            var seguimientos = await _context.OrdenesSeguimiento
                .Where(o => ordenes.Contains(o.Nopedido))
                .OrderBy(o => o.RegistradoEn)
                .ToListAsync(ct);

            foreach (var grupo in seguimientos.GroupBy(s => s.Nopedido))
            {
                var lista = grupo.OrderBy(s => s.RegistradoEn).ToList();
                var ultimo = lista[^1];

                // desactivar TODOS los seguimientos de esta orden
                foreach (var seg in lista)
                {
                    seg.EstadoActivo = false;
                }

                // agregar un nuevo seguimiento con la orden DESBLOQUEADA / ACTIVA
                var nuevoSeguimiento = new OrdenSeguimiento
                {
                    Nopedido = ultimo.Nopedido,
                    Evento = tipoOperacion == TipoOperacion.BLOQUEADA ? "BLOQUEADA": "DESBLOQUEADA",
                    Descripcion = descripcion,
                    EstadoActivo = true,
                    RegistradoEn = TimeHelper.NowMexicoUnspecified(),
                    UsuarioModifico = usuarioId,
                };

                await _context.OrdenesSeguimiento.AddAsync(nuevoSeguimiento, ct);
            }

            await _context.SaveChangesAsync(ct);
        }

    }
}

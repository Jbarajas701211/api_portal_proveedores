using ApiProveedores.Helper;
using ApiProveedores.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProveedores.Services.Citas
{
    public class CantidadesTeoricasService
    {
        private readonly PortalDbContext _context;

        public CantidadesTeoricasService(PortalDbContext context)
        {
            _context = context;
        }

        public async Task ActualizaCantidadesTeoricasAsync(
            Cita cita, bool positive = true,
            CancellationToken ct = default)
        {
            if (!cita.Detalles.Any()) return;

            // obtiene las ordenes de compra.
            var ordenes = cita.Detalles
                .Select(d => d.Oc)
                .Where(oc => !string.IsNullOrWhiteSpace(oc))
                .Select(oc => oc.Trim())
                .Distinct()
                .ToList();

            var filas = await _context.Ordenes
                .AsNoTracking()
                .Where(o => ordenes.Contains(o.Nopedido))
                .Select(o => new { o.Nopedido, o.Cantitotal })
                .ToListAsync(ct);


            var cantidadesPorOc = filas.ToDictionary(
                x => x.Nopedido,
                x => x.Cantitotal ?? 0m
            );

            foreach (var d in cita.Detalles)
            {
                var cantidadTotal = cantidadesPorOc.GetValueOrDefault(d.Oc);
                await ActualizaCantidadesTeoricasAsync(
                    d.Oc, 
                    (int) cantidadTotal,
                    positive ? d.CantidadPorCita : d.CantidadPorCita * -1, ct);
            }
        }

        private async Task<OrdenCantidadTeorica> ActualizaCantidadesTeoricasAsync(
            string noOrden, int cantidadTotal, int cantidadTeorica, CancellationToken ct = default)
        {

            var entity = await _context.OrdenCantidadTeorica
                .FirstOrDefaultAsync(x => x.Oc == noOrden, ct);

            if (entity == null)
            {
                // crea nueva cantidad teorica para la orden
                entity = new OrdenCantidadTeorica
                {
                    Oc = noOrden,
                    CantidadTeorica = cantidadTeorica,
                    CantidadTotal = cantidadTotal,
                    RegistradoEn = TimeHelper.NowMexicoUnspecified()
                };

                _context.OrdenCantidadTeorica.Add(entity);
            }
            else
            {
                // actualizar cantidades teoricas existente
                _context.Attach(entity);

                entity.CantidadTeorica = entity.CantidadTeorica + cantidadTeorica;
                entity.ActualizadoEn = TimeHelper.NowMexicoUnspecified();

                _context.Entry(entity).Property(e => e.CantidadTeorica).IsModified = true;
                _context.Entry(entity).Property(e => e.ActualizadoEn).IsModified = true;
            }

            await _context.SaveChangesAsync(ct);

            return entity;
        }

    }
}

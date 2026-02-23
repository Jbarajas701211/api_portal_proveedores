using ApiProveedores.Models;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ApiProveedores.Services.Helper
{
    public class HelperCita
    {
        private readonly PortalDbContext _context;

        public HelperCita(PortalDbContext context)
        {
            _context = context;
        }
        public async Task<int> CantidadCitaAsync(
            Cita cita,
            CancellationToken ct = default)
        {
            if (cita == null)
                throw new ArgumentNullException(nameof(cita));

            var total = await _context.CitasDetalle
                .Where(d => d.CitaId == cita.Id)
                .SumAsync(d => (int?)d.CantidadPorCita, ct)
                ?? 0;

            return total;
        }

    }
}

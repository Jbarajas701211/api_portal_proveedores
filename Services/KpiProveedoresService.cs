using ApiProveedores.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.EntityFrameworkCore;
using ApiProveedores.Helper;

namespace ApiProveedores.Services
{
    public class KpiProveedoresService
    {
        private readonly PortalDbContext _db;

        public KpiProveedoresService(PortalDbContext db)
        {
            _db = db;
        }

        public async Task<List<KpiProveedorResult>> ObtenerKpisAsync(
            DateTime? desde,
            DateTime? hasta,
            long? proveedorId = null,
            CancellationToken ct = default)
        {
            DateTime? d = null;
            DateTime? h = null;

            if (desde.HasValue)
                d = DateTime.SpecifyKind(desde.Value.Date, DateTimeKind.Utc);

            if (hasta.HasValue)
                h = TimeHelper.UtcNow();

            return await _db.KpiProveedores
                .FromSqlInterpolated($@"
                    SELECT *
                    FROM portal.kpi_proveedores({(object?)d}::date, {(object?)h}::date, {proveedorId})
                ")
                .ToListAsync(ct);
        }




    }

}

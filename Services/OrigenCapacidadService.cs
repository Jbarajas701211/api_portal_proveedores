using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProveedores.Services
{
    public class OrigenCapacidadService
    {
        private readonly PortalDbContext _context;
        private readonly IMemoryCache _cache;

        private const string CACHE_KEY_CATALOGO = "CatalogoOrigenCapacidad:All";

        public OrigenCapacidadService(PortalDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<string?> ObtenerClavePorOrigenAsync(string claveOrigen, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(claveOrigen))
                return null;

            var claveOrigenNorm = claveOrigen.Trim().ToUpperInvariant();

            var diccionario = await _cache.GetOrCreateAsync(
                CACHE_KEY_CATALOGO,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                    entry.SlidingExpiration = TimeSpan.FromMinutes(5);

                    var items = await _context.CatalogoOrigenCapacidad
                        .AsNoTracking()
                        .Where(x => x.Activo)
                        .ToListAsync(ct);

                    var dict = items
                        .Where(x => !string.IsNullOrWhiteSpace(x.ClaveOrigen))
                        .GroupBy(x => x.ClaveOrigen!.Trim().ToUpperInvariant())
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.Clave)
                                  .First(),
                            StringComparer.OrdinalIgnoreCase
                        );

                    return dict;
                });

            if (diccionario.TryGetValue(claveOrigenNorm, out var clave))
                return clave;

            return null;
        }

    }
}

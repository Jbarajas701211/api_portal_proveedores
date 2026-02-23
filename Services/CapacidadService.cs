using ApiProveedores.Dto.Entrada;
using ApiProveedores.Helper;
using ApiProveedores.Models;
using ApiProveedores.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiProveedores.Services
{
    public enum TipoCapacidad
    {
        ASIGNACION,
        LIBERACION
    }

    public class CapacidadService
    {
        private readonly PortalDbContext _context;
        private readonly IMemoryCache _cache;
        private string CACHE_KEY = string.Empty;

        public CapacidadService(PortalDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task RegistrarCapacidadUsoAsync(
            string cd,
            string claveAlmacen,
            DateTime fecha,
            int cantidad,
            string tipo,
            int usuarioId
        )
        {
            // validar que el CD y origen existen
            var existe = await _context.CapacidadCdOrigen
                .AnyAsync(c => c.Cd == cd && c.Origen == claveAlmacen);

            if (!existe)
            {
                throw new CapacidadException($"No se encontró configuración de capacidad para CD '{cd}' y origen '{claveAlmacen}'.");
            }

            // crear registro de uso de capacidad

            var uso = new CapacidadUso
            {
                Cd = cd,
                Origen = claveAlmacen,
                Fecha = fecha.Date,
                CantidadAsignada = Math.Abs(cantidad),
                Tipo = Enum.Parse<TipoCapacidad>(tipo, ignoreCase: true),
                UsuarioId = usuarioId,
                RegistradoEn = TimeHelper.NowMexicoUnspecified()
            };

            _context.CapacidadUso.Add(uso);

            _cache.Remove(CACHE_KEY);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new CapacidadException("No se pudo registrar la capacidad. Verifica las restricciones.", ex);
            }
        }

        public async Task<int?> ObtenerCapacidadMaximaSiExisteAsync(string cd, string origen)
        {
            CACHE_KEY = $"capacidad_maxima_{cd}_{origen}";

            if (_cache.TryGetValue(CACHE_KEY, out int capacidadMaxima))
            {
                return capacidadMaxima;
            }

            var capacidad = await _context.CapacidadCdOrigen
                .Where(c => c.Cd == cd && c.Origen == origen)
                .Select(c => (int?)c.CapacidadMaxima)
                .FirstOrDefaultAsync();

            if (capacidad.HasValue)
            {
                _cache.Set(CACHE_KEY, capacidad.Value, TimeSpan.FromMinutes(60));
            }
            return capacidad;
        }



        public async Task<List<ResumenCapacidadDto>> ObtenerResumenCapacidadAsync(
            string? centroDistribucion = null,
            string? origen = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null)
        {
            var query = _context.CapacidadResumenCd.AsQueryable();

            if (!string.IsNullOrWhiteSpace(centroDistribucion))
                query = query.Where(r => r.Cd == centroDistribucion);

            if (!string.IsNullOrWhiteSpace(origen))
                query = query.Where(r => r.Origen == origen);

            if ((fechaInicio.HasValue && !fechaFin.HasValue) || (!fechaInicio.HasValue && fechaFin.HasValue))
                throw new CapacidadException("Ambas fechas deben ser indicadas si se usa un rango.");

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var inicioUtc = DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc);
                var finUtc = DateTime.SpecifyKind(fechaFin.Value.Date, DateTimeKind.Utc);
                query = query.Where(r => r.Fecha >= inicioUtc && r.Fecha <= finUtc);
            }

            var resumenConsolidado = await query
                .GroupBy(r => new { r.Cd, r.Origen, r.CapacidadMaxima })
                .Select(g => new ResumenCapacidadDto
                {
                    Cd = g.Key.Cd,
                    Origen = g.Key.Origen,
                    CapacidadMaxima = g.Key.CapacidadMaxima,
                    CapacidadUtilizada = g.Sum(r => r.CapacidadUtilizada),
                    CapacidadDisponible = g.Sum(r => r.CapacidadDisponible),
                    ActualizadoEn = g.Max(r => r.ActualizadoEn)
                })
                .OrderBy(r => r.Cd)
                .ThenBy(r => r.Origen)
                .ToListAsync();

            if (resumenConsolidado.Any())
            {
                return resumenConsolidado;
            }

            if (!string.IsNullOrWhiteSpace(centroDistribucion) || !string.IsNullOrWhiteSpace(origen))
            {
                var capacidadMaxima = await ObtenerCapacidadMaximaSiExisteAsync(centroDistribucion, origen) ?? 0;

                return new List<ResumenCapacidadDto>
                {
                    new ResumenCapacidadDto
                    {
                        Cd = centroDistribucion,
                        Origen = origen,
                        CapacidadMaxima = capacidadMaxima,
                        CapacidadUtilizada = 0,
                        CapacidadDisponible = capacidadMaxima,
                        ActualizadoEn = DateTime.UtcNow
                    }
                };
            }

            return new List<ResumenCapacidadDto>();
        }

        public async Task<List<CapacidadDiaDto>> ObtenerCapaciadPorDiaAsync(string? centroDistribucion = null)
        {
            var fechaInicial = DateTime.UtcNow.Date;
            var fechaFinalExclusiva = fechaInicial.AddDays(7);

            var cdOrigenQuery = _context.CapacidadCdOrigen.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(centroDistribucion))
                cdOrigenQuery = cdOrigenQuery.Where(x => x.Cd == centroDistribucion);

            var cdOrigen = await cdOrigenQuery
                .Select(x => new { x.Cd, x.Origen, x.CapacidadMaxima })
                .ToListAsync();

            var origenDescMap = await _context.CatalogoOrigenCapacidad.AsNoTracking()
                .Where(x => x.Activo)
                .Select(x => new { x.Clave, x.Descripcion })
                .ToDictionaryAsync(x => x.Clave, x => x.Descripcion);

            var resumenAgg = await _context.CapacidadResumenCd.AsNoTracking()
                .Where(r => r.Fecha >= fechaInicial && r.Fecha < fechaFinalExclusiva)
                .Where(r => string.IsNullOrWhiteSpace(centroDistribucion) || r.Cd == centroDistribucion)
                .GroupBy(r => new { r.Cd, r.Origen, Dia = r.Fecha.Date, r.CapacidadDisponible })
                .Select(g => new
                {
                    g.Key.Cd,
                    g.Key.Origen,
                    Dia = g.Key.Dia,
                    Utilizada = g.Sum(x => x.CapacidadUtilizada),
                    FechaMax = g.Max(x => x.Fecha),
                    Disponible = g.Key.CapacidadDisponible
                })
                .ToListAsync();

            var map = resumenAgg.ToDictionary(x => (x.Cd, x.Origen, x.Dia), x => x);

            var dias = Enumerable.Range(0, 7).Select(i => fechaInicial.AddDays(i)).ToArray();
            var salida = new List<CapacidadDiaDto>(cdOrigen.Count * dias.Length);

            var cdOrigenOrdenado = cdOrigen
                .OrderBy(x => x.Cd)
                .ThenBy(x => x.Origen)
                .ToList();

            foreach (var cco in cdOrigenOrdenado)
            {
                var origenClave = cco.Origen;
                origenDescMap.TryGetValue(origenClave, out var origenDescripcion);
                var acumulaDisponible = 0;
                var contieneDisponible = false;

                foreach (var dia in dias)
                {
                    map.TryGetValue((cco.Cd, origenClave, dia), out var r);

                    var utilizada = r?.Utilizada ?? 0;
                    var disponible = r?.Disponible ?? cco.CapacidadMaxima;
                    if(r?.Disponible > 0)
                    {
                        acumulaDisponible = r.Disponible;
                        contieneDisponible = true;
                    }

                    salida.Add(new CapacidadDiaDto
                    {
                        Cd = cco.Cd,
                        Origen = origenClave,
                        OrigenDescripcion = origenDescripcion,
                        CapacidadMaxima = cco.CapacidadMaxima,
                        CapacidadUtilizada = utilizada,
                        CapacidadDisponible = contieneDisponible && r?.Disponible is null ? acumulaDisponible : disponible,
                        Fecha = r?.FechaMax ?? dia
                    });
                }
            }

            return salida
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.Cd)
                .ThenBy(x => x.Origen)
                .ToList();
        }

    }
}

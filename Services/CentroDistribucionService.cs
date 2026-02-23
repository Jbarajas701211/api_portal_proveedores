using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProveedores.Services
{
    public class CentroDistribucionService
    {
        private readonly PortalDbContext _context;
        private readonly IMemoryCache _cache;

        private const string KeyPrefix = "cd:exists:";
        private const string KeyPrefixList = "cd:list:";

        private static readonly TimeSpan TtlHit = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan TtlMiss = TimeSpan.FromMinutes(1);

        public CentroDistribucionService(PortalDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IReadOnlyList<CentroDistribucionDto>> ObtenerTodosAsync(CancellationToken ct = default)
        {
            var cacheKey = KeyPrefixList + "all";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                var query = _context.CentrosDistribucion.AsNoTracking();
                var items = await query
                    .OrderBy(c => c.Clave)
                    .Where(c => c.Activo)
                    .Select(c => new CentroDistribucionDto
                    {
                        Clave = c.Clave,
                        Nombre = c.Nombre,
                        Activo = c.Activo
                    })
                    .ToListAsync(ct);

                entry.AbsoluteExpirationRelativeToNow = TtlHit;
                return (IReadOnlyList<CentroDistribucionDto>)items;
            });
        }

        public async Task<ApiResponseDto> CrearCentroDistribucionAsync(CentroDistribucionDto dto)
        {
            var clave = Normalizar(dto.Clave);
            if (string.IsNullOrEmpty(clave))
            {
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "La clave del centro de distribución no puede estar vacía."
                };
            }
            var existe = await ExisteAsync(clave);
            if (existe)
            {
                await ActualizarCentroDistribucionAsync(dto);
                return new ApiResponseDto
                {
                    Success = true,
                    Message = "El centro de distribución ya existe. Fue reactivado"
                };
            }
            var nuevoCentro = new CentroDistribucion
            {
                Clave = clave,
                Nombre = dto.Nombre?.Trim(),
                Activo = true
            };
            try
            {
                _context.CentrosDistribucion.Add(nuevoCentro);
                await _context.SaveChangesAsync();
                Invalidate(clave);
                _cache.Remove(KeyPrefixList + "all");
                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Centro de distribución creado exitosamente."
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDto
                {
                    Success = false,
                    Message = $"Error al crear el centro de distribución: {ex.Message}"
                };
            }
            
        }

        public async Task<ApiResponseDto> ActualizarCentroDistribucionAsync(CentroDistribucionDto dto)
        {
            var clave = Normalizar(dto.Clave);
            if (string.IsNullOrEmpty(clave))
            {
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "La clave del centro de distribución no puede estar vacía."
                };
            }
            var centroExistente = await _context.CentrosDistribucion
                .FirstOrDefaultAsync(c => c.Clave == clave);
            if (centroExistente == null)
            {
                return new ApiResponseDto
                {
                    Success = false,
                    Message = "El centro de distribución no existe."
                };
            }
            centroExistente.Nombre = dto.Nombre?.Trim();
            centroExistente.Activo = dto.Activo;
            try
            {
                await _context.SaveChangesAsync();
                Invalidate(clave);
                _cache.Remove(KeyPrefixList + "all");
                return new ApiResponseDto
                {
                    Success = true,
                    Message = "Centro de distribución actualizado exitosamente."
                };
            }
            catch (Exception ex)
            {
                return new ApiResponseDto
                {
                    Success = false,
                    Message = $"Error al actualizar el centro de distribución: {ex.Message}"
                };
            }
        }

        public async Task<bool> ExisteAsync(string cd, CancellationToken ct = default)
        {
            var clave = Normalizar(cd);
            if (string.IsNullOrEmpty(clave)) return false;

            var cacheKey = KeyPrefix + clave;

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                var existe = await _context.CentrosDistribucion
                    .AsNoTracking()
                    .AnyAsync(c => c.Clave == clave, ct);

                entry.AbsoluteExpirationRelativeToNow = existe ? TtlHit : TtlMiss;

                return existe;
            });
        }

        public void Invalidate(string cd)
        {
            var clave = Normalizar(cd);
            if (!string.IsNullOrEmpty(clave))
                _cache.Remove(KeyPrefix + clave);
        }

        private static string Normalizar(string? cd)
            => (cd ?? string.Empty).Trim().ToUpperInvariant();
    }
}

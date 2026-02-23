using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Helper;
using ApiProveedores.Models;
using ApiProveedores.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProveedores.Services
{
    public class NotificacionesService
    {
        private readonly PortalDbContext _context;
        private readonly IMemoryCache _cache;

        public NotificacionesService(PortalDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<long> CrearNotificacionAsync(DateTime fecha, TimeSpan hora, string titulo, string tag, string detalle,
            List<long>? usuarioIds = null, string? rol = null, long? proveedorId = null, string? metadata = "{}")
        {
            // obtiene destinatarios
            var query = _context.Usuarios.AsQueryable();

            if (usuarioIds != null && usuarioIds.Any())
            {
                query = query.Where(u => usuarioIds.Contains(u.IdUsuario));
            }
            else if (!string.IsNullOrWhiteSpace(rol))
            {
                //query = query.Where(u => u.Rol == rol);
            }
            else if (proveedorId.HasValue)
            {
                //query = query.Where(u => u.ProveedorId == proveedorId);
            }
            else
            {
                throw new NotificationException("Debe especificarse usuarioIds, rol o proveedorId");
            }

            var destinatarios = await query.ToListAsync();

            if (!destinatarios.Any())
                throw new NotificationException("No se encontraron usuarios para notificar");

            // crea notificacion
            var notificacion = new Notificacion
            {
                Fecha = TimeHelper.UtcNow(),
                Hora = hora,
                Titulo = titulo,
                Tag = tag,
                Detalle = detalle,
                MetaData = metadata,
                CreadoEn = TimeHelper.NowMexicoUnspecified()
            };

            _context.Add(notificacion);
            await _context.SaveChangesAsync();

            // asocia a los usuarios
            var relaciones = destinatarios.Select(u => new NotificacionUsuario
            {
                NotificacionId = notificacion.Id,
                UsuarioId = u.IdUsuario,
                Leida = false
            });

            _context.NotificacionesUsuarios.AddRange(relaciones);
            await _context.SaveChangesAsync();

            return notificacion.Id;
        }

        public async Task<ResultadoPaginado<NotificacionDto>> ObtenerNotificacionesPorUsuarioAsync(long usuarioId, int pagina = 1, int tamano = 15)
        {
            var query = _context.NotificacionesUsuarios
                .Where(e => e.UsuarioId == usuarioId)
                .Include(e => e.Notificacion)
                .OrderByDescending(e => e.Notificacion.CreadoEn);

            var totalElementos = await query.CountAsync();

            var elementos = await query
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .Select(e => new NotificacionDto
                {
                    Id = e.Notificacion.Id,
                    Titulo = e.Notificacion.Titulo,
                    Tag = e.Notificacion.Tag,
                    Detalle = e.Notificacion.Detalle,
                    Fecha = e.Notificacion.Fecha,
                    Hora = e.Notificacion.Hora,
                    Leida = e.Leida,
                    LeidaEn = e.LeidaEn
                })
                .ToListAsync();

            return new ResultadoPaginado<NotificacionDto>
            {
                PaginaActual = pagina,
                TotalPaginas = (int)Math.Ceiling(totalElementos / (double)tamano),
                TotalElementos = totalElementos,
                Elementos = elementos
            };
        }


        public async Task<ResultadoPaginado<NotificacionDto>> ObtenerNotificacionesPorUsuarioAsync(
            long usuarioId,
            int pagina = 1,
            int tamano = 15,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null)
        {
            if (pagina < 1) pagina = 1;
            if (tamano <= 0) tamano = 15;

            var query = _context.NotificacionesUsuarios
                .AsNoTracking()
                .Where(e => e.UsuarioId == usuarioId);


            if (fechaDesde.HasValue && fechaHasta.HasValue) {


                var inicioUtc = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);

                fechaHasta.Value.Date.AddDays(1);
                var finUtc = DateTime.SpecifyKind(fechaHasta.Value.Date, DateTimeKind.Utc);


                inicioUtc = DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc);
                finUtc = DateTime.SpecifyKind(finUtc, DateTimeKind.Utc);

                query = query.Where(e => e.Notificacion.Fecha >= inicioUtc && e.Notificacion.Fecha < finUtc);
            }

            query = query
                .Include(e => e.Notificacion)
                .OrderByDescending(e => e.Notificacion.Fecha)
                .ThenByDescending(e => e.Notificacion.Hora);

            var totalElementos = await query.CountAsync();

            var elementos = await query
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .Select(e => new NotificacionDto
                {
                    Id = e.Notificacion.Id,
                    Titulo = e.Notificacion.Titulo,
                    Tag = e.Notificacion.Tag,
                    Detalle = e.Notificacion.Detalle,
                    Fecha = e.Notificacion.Fecha,
                    Hora = e.Notificacion.Hora,
                    Leida = e.Leida,
                    LeidaEn = e.LeidaEn
                })
                .ToListAsync();

            return new ResultadoPaginado<NotificacionDto>
            {
                PaginaActual = pagina,
                TotalPaginas = (int)Math.Ceiling(totalElementos / (double)tamano),
                TotalElementos = totalElementos,
                Elementos = elementos
            };
        }


        public async Task<(DateTime? MaxUpdatedAt, int Total)> ObtenerFirmaUsuarioAsync(
            long usuarioId,
            CancellationToken ct = default)
        {

            var filas = await (
                from nu in _context.NotificacionesUsuarios.AsNoTracking()
                join n in _context.Notificaciones.AsNoTracking()
                    on nu.NotificacionId equals n.Id
                where nu.UsuarioId == usuarioId
                select new
                {
                    LeidaEn = nu.LeidaEn,
                    CreadoEn = n.CreadoEn
                }
            ).ToListAsync(ct);

            if (filas.Count == 0)
                return (null, 0);

            var sig = filas
                .Select(x => new
                {
                    UpdatedAt = (DateTime?)(x.LeidaEn ?? x.CreadoEn)
                })
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    MaxUpdatedAt = g.Max(x => x.UpdatedAt),
                    Total = g.Count()
                })
                .FirstOrDefault();

            return (sig?.MaxUpdatedAt, sig?.Total ?? 0);
        }


        public async Task<List<NotificacionDto>> ObtenerNotificacionesUsuarioAsync(
            long usuarioId,
            DateTime? since,
            int take = 15,
            CancellationToken ct = default)
        {

            var rows = await (
                from nu in _context.NotificacionesUsuarios.AsNoTracking()
                join n in _context.Notificaciones.AsNoTracking()
                    on nu.NotificacionId equals n.Id
                where nu.UsuarioId == usuarioId && !nu.Leida
                orderby n.CreadoEn descending
                select new
                {
                    n.Id,
                    n.Titulo,
                    n.Tag,
                    n.Detalle,
                    n.Fecha,
                    n.Hora,
                    nu.Leida,
                    nu.LeidaEn,
                    n.CreadoEn
                }
            ).ToListAsync(ct);

            var q = rows
                .Select(x => new
                {
                    x.Id,
                    x.Titulo,
                    x.Tag,
                    x.Detalle,
                    x.Fecha,
                    x.Hora,
                    x.Leida,
                    x.LeidaEn,
                    UpdatedAt = (DateTime?)(x.LeidaEn ?? x.CreadoEn)
                });

            if (since is not null)
            {
                q = q.Where(x => x.UpdatedAt > since.Value);
            }

            var items = q
                .OrderByDescending(x => x.UpdatedAt)
                .ThenBy(x => x.Id)
                .Take(Math.Clamp(take, 1, 200))
                .Select(x => new NotificacionDto
                {
                    Id = x.Id,
                    Titulo = x.Titulo,
                    Tag = x.Tag,
                    Detalle = x.Detalle,
                    Fecha = x.Fecha,
                    Hora = x.Hora,
                    Leida = x.Leida,
                    LeidaEn = x.LeidaEn
                })
                .ToList();

            return items;
        }


        public async Task<List<NotificacionDto>> ObtenerUltimasNotificacionesPorUsuarioAsync(long usuarioId)
        {
            var cacheKey = $"ultimas_notificaciones_usuario_{usuarioId}";

            if (!_cache.TryGetValue(cacheKey, out List<NotificacionDto> notificaciones))
            {
                notificaciones = await _context.NotificacionesUsuarios
                    .Where(e => e.UsuarioId == usuarioId)
                    .Include(e => e.Notificacion)
                    .OrderByDescending(e => e.Notificacion.CreadoEn)
                    .Take(15)
                    .Select(e => new NotificacionDto
                    {
                        Id = e.Notificacion.Id,
                        Titulo = e.Notificacion.Titulo,
                        Tag = e.Notificacion.Tag,
                        Detalle = e.Notificacion.Detalle,
                        Fecha = e.Notificacion.Fecha,
                        Hora = e.Notificacion.Hora,
                        Leida = e.Leida,
                        LeidaEn = e.LeidaEn
                    })
                    .ToListAsync();

                _cache.Set(cacheKey, notificaciones, TimeSpan.FromMinutes(2));
            }

            return notificaciones;
        }

        public async Task<int> MarcarLeidaAsync(long notificacionId, long usuarioId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var afectados = await _context.NotificacionesUsuarios
                .Where(nu => nu.NotificacionId == notificacionId
                             && nu.UsuarioId == usuarioId
                             && !nu.Leida)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.Leida, true)
                    .SetProperty(x => x.LeidaEn, now), ct);

            return afectados;
        }

    }
}

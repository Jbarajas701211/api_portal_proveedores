using ApiProveedores.Dto;
using ApiProveedores.Dto.Mappers;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;
using ApiProveedores.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ApiProveedores.Services
{
    public class OrdenService
    {
        private readonly PortalDbContext _context;
        private readonly ProveedoresService _proveedorService;

        public OrdenService(PortalDbContext context, ProveedoresService proveedorService)
        {
            _context = context;
            _proveedorService = proveedorService;
        }

        public async Task<Orden> RecuperaOrdenAsync(string noPedido, long idProveedor)
        {
            if (string.IsNullOrWhiteSpace(noPedido))
                throw new CitaException("El numero de orden no puede estar vacío.");

            var proveedor = await _proveedorService.RecuperaProveedorAsync(idProveedor);
            var orden = await _context.Ordenes
              .FirstOrDefaultAsync(o => o.Nopedido == noPedido );

            return orden;
        }

        public async Task<Orden> RecuperaOrdenAsync(string noPedido, string cveProveedor)
        {
            if (string.IsNullOrWhiteSpace(noPedido))
                throw new CitaException("El numero de orden no puede estar vacío.");

            var proveedor = await _proveedorService.RecuperaProveedorAsync(cveProveedor);
            var orden = await _context.Ordenes
              .FirstOrDefaultAsync(o => o.Nopedido == noPedido );

            return orden;
        }

        public async Task<Orden> RecuperaOrdenAsync(string noPedido)
        {
            if (string.IsNullOrWhiteSpace(noPedido))
                throw new CitaException("El numero de orden no puede estar vacío.");

            var orden = await _context.Ordenes
              .FirstOrDefaultAsync(o => o.Nopedido == noPedido);

            return orden;
        }

        public async Task<ResultadoPaginado<OrdenDto>> ConsultarOrdenesAsync(FiltroOrdenDto filtro, string rol)
        {
            var query = _context.Ordenes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro.NumeroOrden))
            {
                query = query.Where(o => o.Nopedido.Contains(filtro.NumeroOrden));
                if (rol == "PROVEEDOR")
                {
                    if (string.IsNullOrWhiteSpace(filtro.Proveedor))
                        throw new OrdenException("El proveedor es obligatorio para realizar la búsqueda.");

                    var proveedor = await _proveedorService.RecuperaProveedorAsync(long.Parse(filtro.Proveedor));

                    query = query.Where(o => o.Cveprov == "1");
                }
            }
            else {
                if (!string.IsNullOrWhiteSpace(filtro.Proveedor))
                {
                    var proveedor = await _proveedorService.RecuperaProveedorAsync(long.Parse(filtro.Proveedor));
                    query = query.Where(o => o.Cveprov == "1");
                }
            }


            if (!string.IsNullOrWhiteSpace(filtro.CentroDistribucion))
                query = query.Where(o => o.Cd == filtro.CentroDistribucion);

            if (!string.IsNullOrWhiteSpace(filtro.TipoOrden))
                query = query.Where(o => o.Unnego == filtro.TipoOrden);

            if (!string.IsNullOrEmpty(filtro.Estatus)) 
            {
                if (filtro.Estatus == "NO_COMPLETADA") 
                {
                    query = query.Where(o => o.Status == 0);
                } 
                else if (filtro.Estatus == "COMPLETADA")
                {
                    query = query.Where(o => o.Status == 1);
                }
                else if (filtro.Estatus == "CANCELADA")
                {
                    query = query.Where(o => o.Notacanc == 1);
                }
            }

            if (filtro.FechaRegistroInicio.HasValue && filtro.FechaRegistroFin.HasValue)
            {
                var inicioUtc = DateTime.SpecifyKind(filtro.FechaRegistroInicio.Value.Date, DateTimeKind.Utc);
                var finUtc = DateTime.SpecifyKind(filtro.FechaRegistroFin.Value.Date, DateTimeKind.Utc);

                query = query.Where(r => r.Fechapedido >= inicioUtc && r.Fechapedido <= finUtc);
            }


            if (filtro.FechaVencimientoInicio.HasValue && filtro.FechaVencimientoFin.HasValue)
            {
                var inicioUtc = DateTime.SpecifyKind(filtro.FechaVencimientoInicio.Value.Date, DateTimeKind.Utc);
                var finUtc = DateTime.SpecifyKind(filtro.FechaVencimientoFin.Value.Date, DateTimeKind.Utc);

                query = query.Where(r => r.Fechavenci >= inicioUtc && r.Fechavenci <= finUtc);
            }


            var baseQuery = query;

            var joined = from o in baseQuery
                         join p in _context.Proveedores.AsNoTracking()
                           on o.Cveprov equals "1" into pj
                         from p in pj.DefaultIfEmpty()

                         join cd in _context.Set<CentroDistribucion>().AsNoTracking()
                           on o.Cd equals cd.Clave into cdj
                         from cd in cdj.DefaultIfEmpty()

                         select new
                         {
                             Orden = o,
                             ProveedorNombre = p != null ? p.Nombre : null,
                             CentroDistribucionNombre = cd != null ? cd.Nombre : null,
                         };

            var totalRegistros = await joined.CountAsync();
            var skip = (filtro.Pagina - 1) * filtro.RegistrosPorPagina;

            var ordenesEntidad = await joined
                .OrderByDescending(o => o.Orden.Fechapedido)
                .Skip(skip)
                .Take(filtro.RegistrosPorPagina)
                .ToListAsync();

            var ordenes = ordenesEntidad
                .Select(x => {
                    var dto = x.Orden.ToDto();
                    dto.Proveedor = x.ProveedorNombre;
                    dto.NombreCentroDistribucion = x.CentroDistribucionNombre ?? string.Empty;
                    return dto;
                })
                .ToList();

            return new ResultadoPaginado<OrdenDto>
            {
                PaginaActual = filtro.Pagina,
                TotalPaginas = (int)Math.Ceiling((double)totalRegistros / filtro.RegistrosPorPagina),
                TotalElementos = totalRegistros,
                Elementos = ordenes
            };
        }

    }
}

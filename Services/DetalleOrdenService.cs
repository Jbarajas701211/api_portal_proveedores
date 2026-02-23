using ApiProveedores.Dto;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Helper;
using ApiProveedores.Models;
using ApiProveedores.Services.Exceptions;
using ApiProveedores.Services.PubSub;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ApiProveedores.Services
{
    public class DetalleOrdenService
    {
        private readonly PortalDbContext _context;
        private readonly ProveedoresService _proveedorService;

        public DetalleOrdenService(PortalDbContext context, ProveedoresService proveedorService)
        {
            _context = context;
            _proveedorService = proveedorService;
        }


        public async Task<ResultadoPaginado<DetalleOrden>> ConsultaDetalleOrdenesAsync(FiltroDetalleOrdenDto filtro)
        {
            var query = _context.DetalleOrdenes.AsQueryable();
            query = query.Where(o => o.Nopedido.Contains(filtro.NumeroOrden));

            var totalRegistros = await query.CountAsync();
            var skip = (filtro.Pagina - 1) * filtro.RegistrosPorPagina;

            var ordenes = await query
                .OrderByDescending(o => o.Cvetienda)
                .Skip(skip)
                .Take(filtro.RegistrosPorPagina)
                .ToListAsync();

            return new ResultadoPaginado<DetalleOrden>
            {
                PaginaActual = filtro.Pagina,
                TotalPaginas = (int)Math.Ceiling((double)totalRegistros / filtro.RegistrosPorPagina),
                TotalElementos = totalRegistros,
                Elementos = ordenes
            };
        }

    }
}

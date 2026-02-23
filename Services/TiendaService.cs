using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ApiProveedores.Services
{
    public class TiendaService
    {
        private readonly PortalDbContext _context;

        public TiendaService(PortalDbContext context)
        {
            _context = context;
        }

        public async Task<ResultadoPaginado<TiendaDto>> BuscarTiendasPaginadoAsync(string? filtro, int pagina, int tamanioPagina)
        {

            if (pagina <= 0) pagina = 1;
            if (tamanioPagina <= 0) tamanioPagina = 10;

            var query = _context.Tiendas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.CcCntrCsto.ToString(), $"%{filtro}%") ||
                    EF.Functions.ILike(p.CcScrs, $"%{filtro}%"));
            }

            var total = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling((double)total / tamanioPagina);
            var tiendas = await query
                .OrderBy(p => p.CcScrs)
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .Select(p => new TiendaDto
                {
                    CcCntrCsto = p.CcCntrCsto,
                    CcScrs = p.CcScrs
                })
                .ToListAsync();

            return new ResultadoPaginado<TiendaDto>
            {
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalElementos = total,
                Elementos = tiendas
            };
        }


    }
}

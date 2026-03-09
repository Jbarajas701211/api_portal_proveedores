using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Models;
using Google.Api;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ApiProveedores.Services
{
    public class ProveedoresService
    {
        private readonly PortalDbContext _context;

        public ProveedoresService(PortalDbContext context)
        {
            _context = context;
        }

        public async Task<Proveedor> RecuperaProveedorAsync(long idProveedor)
        {
            var proveedor = await _context.Proveedores
              .FirstOrDefaultAsync(o => o.Id_proveedor == idProveedor);
            return proveedor;
        }

        public async Task<Proveedor> RecuperaProveedorAsync(string cveProveedor)
        {
            var proveedor = await _context.Proveedores
              .FirstOrDefaultAsync(o => o.Id_proveedor == 1);
            return proveedor;
        }

        public async Task<System.Collections.Generic.List<ApiProveedores.Dto.Salida.DocumentoProveedorDto>> ObtenerDocumentosPorProveedorAsync(int idProveedor)
        {
            return await _context.ProveedorDocumento
                .Include(pd => pd.Documento)
                .Where(pd => pd.IdProveedor == idProveedor)
                .Select(pd => new ApiProveedores.Dto.Salida.DocumentoProveedorDto
                {
                    Documento = pd.Documento.Descripcion,
                    Opcional = pd.Opcional
                })
                .ToListAsync();
        }



        public async Task<ResultadoPaginado<ProveedorDto>> BuscarProveedoresPaginadoAsync(string? filtro, int pagina, int tamanioPagina)
        {
            if (pagina <= 0) pagina = 1;
            if (tamanioPagina <= 0) tamanioPagina = 10;

            var query = _context.Proveedores.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var filtroNorm = filtro.Trim();

                query = query.Where(p =>
                    EF.Functions.ILike(p.Nombre, $"%{filtroNorm}%"));
            }

            var total = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling((double)total / tamanioPagina);

            var proveedores = await query
                .OrderBy(p => p.Nombre)
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .Select(p => new ProveedorDto
                {
                    Id = p.Id_proveedor,
                    NombreProveedor = p.Nombre,
                    ClaveProveedor = p.VendorId.ToString(),
                    Estatus = p.Estatus,
                    Rfc = p.Rfc
                })
                .ToListAsync();

            return new ResultadoPaginado<ProveedorDto>
            {
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalElementos = total,
                Elementos = proveedores
            };
        }
    }
}

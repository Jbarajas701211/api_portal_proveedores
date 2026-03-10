using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Models;
using ApiProveedores.Services.Exceptions;
using Google.Api;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        public async Task<List<ApiProveedores.Dto.Salida.DocumentoProveedorDto>> ObtenerDocumentosPorProveedorAsync(long idProveedor)
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
                    Rfc = p.Rfc,
                    Sobrante = p.Sobrante,
                    PorcentajeSobrante = p.PorcentajeSobrante,
                    Faltante = p.Faltante,
                    PorcentajeFaltante = p.PorcentajeFaltante,
                    AplicarTolerancia = p.AplicarTolerancia,
                    IdCategoria = p.IdCategoria,
                    AccredorSinXml = p.AcreedorSinXml,
                    AplicarToleranciaCategoria = p.AplicarToleranciaCategoria,
                    Email = p.EmailProveedor,
                    DocumentoFiscal = p.DocFiscal,
                    Factura = p.Factura,
                    Recepcion = p.Recepcion,
                    Origen = p.Origen,
                    RazonSocial = p.RazonSocial

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

        // Actualiza un proveedor buscando por Id exclusivamente.
        // Devuelve true si se guardó correctamente, false si no existe el registro.
        public async Task<bool> ActualizarProveedorAsync(ProveedorDto dto)
        {
            if (dto == null)
                throw new ApiProveedoresException("Datos de proveedor inválidos.");

            // Asegurarse de que venga un Id válido
            if (dto.Id <= 0)
            {
                return false;
            }

            try
            {
                var existente = await _context.Proveedores.FindAsync(dto.Id);

                if (existente == null)
                    return false;

                // Mapear campos del DTO a la entidad (solo campos esperados)
                existente.Nombre = dto.NombreProveedor ?? existente.Nombre;

                // Ajuste: VendorId es int en el modelo, parsear a int antes de asignar
                if (!string.IsNullOrWhiteSpace(dto.ClaveProveedor))
                {
                    existente.VendorId = dto.ClaveProveedor;
                }

                existente.Estatus = dto.Estatus;
                existente.Rfc = dto.Rfc;
                existente.Sobrante = dto.Sobrante;
                existente.PorcentajeSobrante = dto.PorcentajeSobrante;
                existente.Faltante = dto.Faltante;
                existente.PorcentajeFaltante = dto.PorcentajeFaltante;
                existente.AplicarTolerancia = dto.AplicarTolerancia;
                existente.IdCategoria = dto.IdCategoria == 0 ? 1 : dto.IdCategoria;
                existente.AcreedorSinXml = dto.AccredorSinXml;
                existente.AplicarToleranciaCategoria = dto.AplicarToleranciaCategoria;
                existente.EmailProveedor = dto.Email;
                existente.DocFiscal = dto.DocumentoFiscal;
                existente.Factura = dto.Factura;
                existente.Recepcion = dto.Recepcion;
                existente.Origen = dto.Origen;
                existente.RazonSocial = dto.RazonSocial;

                _context.Proveedores.Update(existente);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException)
            {
                throw new ApiProveedoresException("No se pudo actualizar el registro.");
            }
            catch (Exception)
            {
                throw new ApiProveedoresException("No se pudo actualizar el registro.");
            }
        }
    }
}

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
using ApiProveedores.Dto;

namespace ApiProveedores.Services
{
    public class EmpresaService
    {
        private readonly PortalDbContext _context;

        public EmpresaService(PortalDbContext context)
        {
            _context = context;
        }  

        public async Task<ResultadoPaginado<EmpresaDto>> BuscarEmpresasPaginadoAsync(string? filtro, int pagina, int tamanioPagina)
        {
            if (pagina <= 0) pagina = 1;
            if (tamanioPagina <= 0) tamanioPagina = 10;

            var query = _context.Empresa.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var filtroNorm = filtro.Trim();

                query = query.Where(p =>
                    EF.Functions.ILike(p.Nombre, $"%{filtroNorm}%"));
            }

            var total = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling((double)total / tamanioPagina);

            var empresas = await query
                .OrderBy(p => p.Nombre)
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .Select(e => new EmpresaDto
                {
                    IdEmpresa = e.IdEmpresa,
                    Nombre = e.Nombre,
                    Rfc = e.Rfc,
                    Estatus = e.Estatus,
                    Unidad = e.Unidad
                })
                .ToListAsync();

            return new ResultadoPaginado<EmpresaDto>
            {
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalElementos = total,
                Elementos = empresas
            };
        }

        public async Task<List<EmpresaDto>> BuscarEmpresasAsync()
        {
            return await _context.Empresa.Select(e => new EmpresaDto
            {
                IdEmpresa = e.IdEmpresa,
                Nombre = e.Nombre,
                Rfc = e.Rfc,
                Estatus = e.Estatus,
                Unidad = e.Unidad
            }).ToListAsync();
        }

        public async Task<EmpresaDto> CrearEmpresaAsync(EmpresaDto dto)
        {
            if(dto is null)
            {
                throw new ApiProveedoresException("El objeto EmpresaDto no puede ser nulo.");
            }

            if(string.IsNullOrWhiteSpace(dto.Nombre))
            {
                throw new ApiProveedoresException("El campo 'Nombre' es obligatorio.");
            }

            if(string.IsNullOrWhiteSpace(dto.Rfc))
            {
                throw new ApiProveedoresException("El campo 'Rfc' es obligatorio.");
            }

            if(string.IsNullOrWhiteSpace(dto.Unidad))
            {
                throw new ApiProveedoresException("El campo 'Unidad' es obligatorio.");
            }

            var empresa = new Empresa
            {
                Nombre = dto.Nombre,
                Rfc = dto.Rfc,
                Estatus = dto.Estatus,
                Unidad = dto.Unidad
            };
            _context.Empresa.Add(empresa);
            await _context.SaveChangesAsync();
            dto.IdEmpresa = empresa.IdEmpresa;
            return dto;
        }

        public async Task<EmpresaDto?> ActualizarEmpresaAsync(EmpresaDto dto)
        {
            if (dto is null)
            {
                throw new ApiProveedoresException("El objeto EmpresaDto no puede ser nulo.");
            }

            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                throw new ApiProveedoresException("El campo 'Nombre' es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(dto.Rfc))
            {
                throw new ApiProveedoresException("El campo 'Rfc' es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(dto.Unidad))
            {
                throw new ApiProveedoresException("El campo 'Unidad' es obligatorio.");
            }

            var empresa = await _context.Empresa.FindAsync(dto.IdEmpresa);
            if (empresa == null) return null;
            empresa.Nombre = dto.Nombre;
            empresa.Rfc = dto.Rfc;
            empresa.Estatus = dto.Estatus;
            empresa.Unidad = dto.Unidad;
            await _context.SaveChangesAsync();
            dto.IdEmpresa = empresa.IdEmpresa;
            return dto;
        }

        public async Task<bool> EliminarEmpresaAsync(int id)
        {
            var empresa = await _context.Empresa.FindAsync(id);
            if (empresa == null) return false;
            _context.Empresa.Remove(empresa);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

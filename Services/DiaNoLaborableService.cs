using ApiProveedores.Dto;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Dto.Salida;
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
    public class DiaNoLaborableService
    {
        private readonly PortalDbContext _context;
        private readonly IMemoryCache _cache;

        public DiaNoLaborableService(PortalDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task RegistrarDiaNoLaborableAsync(DateTime fecha, string descripcion)
        {
            bool existe = await _context.DiasNoLaborables.AnyAsync(d => d.Fecha == fecha.Date);
            if (existe)
            {
                throw new DiaNoLaborableException($"La fecha {fecha:yyyy-MM-dd} ya está registrada como día no laborable.");
            }

            var dia = new DiaNoLaborable
            {
                Fecha = fecha.Date,
                Descripcion = descripcion,
                CreadoEn = TimeHelper.NowMexicoUnspecified()
            };

            _context.DiasNoLaborables.Add(dia);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new DiaNoLaborableException("No se pudo registrar el día no laborable. Verifica las restricciones.", ex);
            }
        }

        public async Task<ResultadoPaginado<DiaNoLaborable>> ConsultarTodosAsync(int pagina, int tamanioPagina, int? anio = null)
        {
            if (pagina < 1) pagina = 1;
            if (tamanioPagina < 1) tamanioPagina = 10;

            int anioConsulta = anio ?? DateTime.Now.Year;

            var query = _context.DiasNoLaborables
                .Where(d => d.Fecha.Year == anioConsulta)
                .OrderBy(d => d.Fecha);

            var totalElementos = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalElementos / (double) tamanioPagina);

            var elementos = await query
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .ToListAsync();

            return new ResultadoPaginado<DiaNoLaborable>
            {
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalElementos = totalElementos,
                Elementos = elementos
            };
        }

        public async Task<ApiResponseDto<bool>> ActulizaDiaNoLaborableAsync(DateTime fecha, string descripcion)
        {
            var dia = await _context.DiasNoLaborables.FirstOrDefaultAsync(d => d.Fecha == fecha.Date);
            if (dia == null)
            {
                throw new DiaNoLaborableException("El día no laborable no existe.");
            }
            dia.Descripcion = descripcion;

            try
            {
                await _context.SaveChangesAsync();
                return new ApiResponseDto<bool>
                {
                    Success = true,
                    Message = "Día no laborable actualizado correctamente.",
                    Data = true
                };
            }
            catch (DbUpdateException ex)
            {
                throw new DiaNoLaborableException("No se pudo actualizar el día no laborable. Verifica las restricciones.", ex);
            }
        }


        public async Task<bool> EsNoLaborableAsync(DateTime fecha)
        {
            return await _context.DiasNoLaborables
                .AnyAsync(d => d.Fecha == fecha.Date);
        }

        public async Task EliminarAsync(DateTime fecha)
        {
            var dia = await _context.DiasNoLaborables.FirstOrDefaultAsync(dia => dia.Fecha == fecha);
            if (dia == null)
            {
                throw new DiaNoLaborableException("El día no laborable no existe.");
            }
            try
            {
                _context.DiasNoLaborables.Remove(dia);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw new DiaNoLaborableException("El día no laborable no se pudo eliminar.", ex); ;
            }
            
        }

    }
}

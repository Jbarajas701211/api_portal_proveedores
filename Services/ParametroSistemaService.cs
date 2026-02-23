using ApiProveedores.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using ApiProveedores.Dto.Paginadores;
using System.Linq;
using ApiProveedores.Services.Exceptions;
using ApiProveedores.Services.Helper;
using ApiProveedores.Dto.Entrada;
using ApiProveedores.Helper;

namespace ApiProveedores.Services
{
    public class ParametroSistemaService
    {
        private readonly PortalDbContext _context;

        public ParametroSistemaService(PortalDbContext context)
        {
            _context = context;
        }


        public async Task ActualizarValorParametroAsync(string clave, string nuevoValor)
        {
            if (string.IsNullOrWhiteSpace(clave))
                throw new ParametroSistemaException("La clave del parámetro no puede estar vacía.");

            if (string.IsNullOrWhiteSpace(nuevoValor))
                throw new ParametroSistemaException("El nuevo valor no puede estar vacío.");

            var parametro = await _context.ParametrosSistema.FindAsync(clave);
            if (parametro == null)
                throw new ParametroSistemaException("La clave del parámetro es inválida.");

            parametro.Valor = nuevoValor;
            parametro.ActualizadoEn = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            _context.ParametrosSistema.Update(parametro);
            await _context.SaveChangesAsync();
        }


        public async Task EliminarParametroAsync(string clave)
        {
            if (string.IsNullOrWhiteSpace(clave))
                throw new ParametroSistemaException("Para eliminar el parámetro debe de indicar una clave.");

            var parametro = await _context.ParametrosSistema.FindAsync(clave);
            if (parametro == null)
                throw new ParametroSistemaException("La clave del parámetro es inválida.");

            _context.ParametrosSistema.Remove(parametro);
            await _context.SaveChangesAsync();
        }

        public async Task RegistrarParametroAsync(ParametroSistemaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Clave))
                throw new ParametroSistemaException("El parámetro debe de incluir una clave (con formato en mayusculas).");

            if (string.IsNullOrWhiteSpace(dto.Valor))
                throw new ParametroSistemaException("El parámetro debe de incluir un valor.");

            if (!ParametrosSistemaHelper.EsClaveValida(dto.Clave))
                throw new ParametroSistemaException("La clave del parámetro solo puede contener letras mayúsculas y guiones bajos (_).");

            var existe = await _context.ParametrosSistema.AnyAsync(p => p.Clave == dto.Clave);
            if (existe)
                throw new ParametroSistemaException("El parámetro ya existe con la misma clave.");

            var parametro = new ParametroSistema
            {
                Clave = dto.Clave,
                Valor = dto.Valor.ToUpper(),
                Descripcion = dto.Descripcion,
                ActualizadoEn = TimeHelper.NowMexicoUnspecified()
            };

            await _context.ParametrosSistema.AddAsync(parametro);
            await _context.SaveChangesAsync();
        }


        public async Task<ParametroSistemaDto?> ObtenerParametroAsync(string clave)
        {
            var parametro = await _context.ParametrosSistema.FindAsync(clave);
            if (parametro == null) return null;

            return new ParametroSistemaDto
            {
                Clave = parametro.Clave,
                Valor = parametro.Valor,
                Descripcion = parametro.Descripcion
            };
        }

        public async Task<bool> ActualizarParametroAsync(ParametroSistemaDto dto)
        {
            var parametro = await _context.ParametrosSistema.FindAsync(dto.Clave);
            if (parametro == null)
                return false;

            parametro.Valor = dto.Valor;
            parametro.Descripcion = dto.Descripcion;
            parametro.ActualizadoEn = TimeHelper.NowMexicoUnspecified();

            _context.ParametrosSistema.Update(parametro);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ResultadoPaginado<ParametroSistemaDto>> BuscarParametrosPaginadoAsync(string? filtro, int pagina, int tamanioPagina)
        {

            if (pagina <= 0) pagina = 1;
            if (tamanioPagina <= 0) tamanioPagina = 10;

            var query = _context.ParametrosSistema.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.Clave, $"%{filtro}%") ||
                    EF.Functions.ILike(p.Descripcion, $"%{filtro}%"));
            }

            var total = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling((double)total / tamanioPagina);
            var parametros = await query
                .OrderBy(p => p.Clave)
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .Select(p => new ParametroSistemaDto
                {
                    Clave = p.Clave,
                    Valor = p.Valor,
                    Descripcion = p.Descripcion,
                })
                .ToListAsync();

            return new ResultadoPaginado<ParametroSistemaDto>
            {
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalElementos = total,
                Elementos = parametros
            };
        }
    }
}

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
            parametro.Modificado = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

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
            if (string.IsNullOrWhiteSpace(dto.Codigo))
                throw new ParametroSistemaException("El parámetro debe de incluir una clave (con formato en mayusculas).");

            if (string.IsNullOrWhiteSpace(dto.Valor))
                throw new ParametroSistemaException("El parámetro debe de incluir un valor.");

            var existe = await _context.ParametrosSistema.AnyAsync(p => p.Codigo == dto.Codigo);
            if (existe)
                throw new ParametroSistemaException("El parámetro ya existe con la misma clave.");

            var parametro = new ParametroSistema
            {
                Codigo = dto.Codigo,
                Descripcion = dto.Descripcion,
                Valor = dto.Valor.ToUpper(),
                UnidadMedida = dto.UnidadMedida,
                Notificacion = dto.Notificacion,
                Modificado = null,
                IdUsuario = 1, // Todo: Reemplazar con el ID del usuario autenticado
                Estatus = dto.Estatus,
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
                IdParametro = parametro.IdParametro,
                Codigo = parametro.Codigo,
                Descripcion = parametro.Descripcion,
                Valor = parametro.Valor,
                UnidadMedida = parametro.UnidadMedida,
                Notificacion = parametro.Notificacion,
                Modificado = parametro.Modificado ?? DateTime.MinValue,
                Estatus = parametro.Estatus
            };
        }

        public async Task<bool> ActualizarParametroAsync(ParametroSistemaDto dto)
        {
            var parametro = await _context.ParametrosSistema.FindAsync(dto.IdParametro);
            if (parametro == null)
                return false;

            parametro.Codigo = dto.Codigo;
            parametro.Descripcion = dto.Descripcion;
            parametro.Valor = dto.Valor;
            parametro.UnidadMedida = dto.UnidadMedida;
            parametro.Notificacion = dto.Notificacion;
            parametro.Modificado = TimeHelper.NowMexicoUnspecified();
            parametro.Estatus = dto.Estatus;

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
                    EF.Functions.ILike(p.IdParametro.ToString(), $"%{filtro}%") ||
                    EF.Functions.ILike(p.Descripcion, $"%{filtro}%"));
            }

            var total = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling((double)total / tamanioPagina);
            var parametros = await query
                .OrderBy(p => p.IdParametro)
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .Select(p => new ParametroSistemaDto
                {
                    IdParametro = p.IdParametro,
                    Codigo = p.Codigo,
                    Descripcion = p.Descripcion ?? string.Empty,
                    Valor = p.Valor,
                    UnidadMedida = p.UnidadMedida,
                    Notificacion = p.Notificacion,
                    Estatus = p.Estatus
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

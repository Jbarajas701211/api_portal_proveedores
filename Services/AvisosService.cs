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
using ApiProveedores.Dto.Salida;

namespace ApiProveedores.Services
{
    public class AvisosService
    {
        private readonly PortalDbContext _context;

        public AvisosService(PortalDbContext context)
        {
            _context = context;
        }


        public async Task ActualizarAvisoAsync(AvisoDto avisoDto)
        {
            if (avisoDto.IdAviso == 0)
                throw new AvisoException("El id del aviso no puede estar vacío.");

            if (string.IsNullOrWhiteSpace(avisoDto.Mensaje))
                throw new AvisoException("El mensaje del aviso no puede estar vacío.");

            var aviso = await _context.Avisos.FindAsync(avisoDto.IdAviso);
            if (aviso == null)
                throw new AvisoException("El id del aviso es inválido.");

            aviso.Mensaje = avisoDto.Mensaje;
            aviso.Estatus = avisoDto.Estatus;
            aviso.FechaInicioAviso = DateTime.SpecifyKind(avisoDto.FechaInicioAviso, DateTimeKind.Utc);
            aviso.FechaFinalAviso = DateTime.SpecifyKind(avisoDto.FechaFinalAviso, DateTimeKind.Utc);
            aviso.Categoria = avisoDto.Categoria;
            aviso.FechaCreacion = TimeHelper.UtcNow();

            try
            {
                _context.Avisos.Update(aviso);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw new AvisoException($"Error al actualizar el aviso. Error: {ex.Message}");
            }
           
        }


        public async Task EliminarAvisoAsync(int id)
        {
            var aviso = await _context.Avisos.FindAsync(id);
            if (aviso == null)
                throw new AvisoException("El aviso a eliminar no existe.");

            _context.Avisos.Remove(aviso);
            await _context.SaveChangesAsync();
        }

        public async Task RegistrarAvisoAsync(AvisoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Categoria))
                throw new AvisoException("El aviso debe de incluir una categoría.");

            if (string.IsNullOrWhiteSpace(dto.Mensaje))
                throw new AvisoException("El aviso debe de incluir un mensaje.");

            if(dto.FechaInicioAviso == default)
                throw new AvisoException("El aviso debe de incluir una fecha de inicio válida.");

            if(dto.FechaFinalAviso == default && dto.FechaFinalAviso < dto.FechaInicioAviso)
                throw new AvisoException("La fecha final del aviso no puede ser anterior a la fecha de inicio.");

           var aviso = new Aviso
           {
               Categoria = dto.Categoria,
               Mensaje = dto.Mensaje,
               Estatus = dto.Estatus,
               FechaInicioAviso = DateTime.SpecifyKind(dto.FechaInicioAviso, DateTimeKind.Utc),
               FechaFinalAviso = dto.FechaFinalAviso == default ? DateTime.MaxValue : DateTime.SpecifyKind(dto.FechaFinalAviso, DateTimeKind.Utc),
               FechaCreacion = TimeHelper.UtcNow()
           };

            await _context.Avisos.AddAsync(aviso);
            await _context.SaveChangesAsync();
        }


        public async Task<ParametroSistemaDto?> ObtenerAvisoAsync(string clave)
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

        public async Task<ResultadoPaginado<AvisoDto>> BuscarAvisosPaginadoAsync(string? filtro, int pagina, int tamanioPagina)
        {

            if (pagina <= 0) pagina = 1;
            if (tamanioPagina <= 0) tamanioPagina = 10;
            try
            {
                var query = _context.Avisos.AsQueryable();

                if (!string.IsNullOrWhiteSpace(filtro))
                {
                    query = query.Where(a =>
                        EF.Functions.ILike(a.IdAviso.ToString(), $"%{filtro}%") ||
                        EF.Functions.ILike(a.Mensaje, $"%{filtro}%") ||
                        EF.Functions.ILike(a.Categoria, $"%{filtro}%"));
                }

                var total = await query.CountAsync();
                var totalPaginas = (int)Math.Ceiling((double)total / tamanioPagina);
                var parametros = await query
                    .OrderBy(a => a.IdAviso)
                    .Skip((pagina - 1) * tamanioPagina)
                    .Take(tamanioPagina)
                    .Select(a => new AvisoDto
                    {
                        IdAviso = a.IdAviso,
                        Categoria = a.Categoria ?? string.Empty,
                        Mensaje = a.Mensaje ?? string.Empty,
                        FechaFinalAviso = a.FechaFinalAviso,
                        FechaInicioAviso = a.FechaInicioAviso,
                        Estatus = a.Estatus
                    })
                    .ToListAsync();

                return new ResultadoPaginado<AvisoDto>
                {
                    PaginaActual = pagina,
                    TotalPaginas = totalPaginas,
                    TotalElementos = total,
                    Elementos = parametros
                };
            }
            catch (Exception ex)
            {

                throw new AvisoException(ex.Message);
            }
            
        }
    }
}

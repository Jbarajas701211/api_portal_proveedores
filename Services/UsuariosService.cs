using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Services.Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AuthService;

namespace ApiProveedores.Services
{
    public enum AgrupadorRol
    {
        PROVEEDORES,
        ONEST,
        LOGISTICA
    }

    public class UsuariosService
    {
        private readonly HelperTraceService _helperTraceService;
        private readonly PortalDbContext _context;

        public UsuariosService(PortalDbContext context, HelperTraceService helperTraceService)
        {
            _context = context;
            _helperTraceService = helperTraceService;
        }

        public async Task<UsuarioDto?> ObtenerUsuarioPorIdAsync(long id)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.IdUsuario == id)
                .Select(u => new UsuarioDto
                {
                    Id = u.IdUsuario,
                    Email = u.CorreoElectronico,
                    NombreCompleto = u.Nombre,
                    //Rol = u.Rol,
                    //ProveedorId = u.ProveedorId,
                    Activo = u.Estatus,
                    Habilitado = u.Estatus
                })
                .FirstOrDefaultAsync();
        }

        public async Task DesactivarUsuarioAsync(HabilitarUsuarioDto dto) {
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == dto.IdUsuario);
            if (user == null)
                return;

            user.Estatus = dto.Habilitado;

            _context.Usuarios.Update(user);
            await _context.SaveChangesAsync();

            // Genera evento de usuario
            if (user.Estatus)
            {
                await _helperTraceService.SaveTraceUsuarios(user.IdUsuario, EventoUsuario.CuentaHabilitadaPorLogistica);
            }
            else 
            {
                await _helperTraceService.SaveTraceUsuarios(user.IdUsuario, EventoUsuario.CuentaInhabilitadaPorLogistica);
            }
            
        }

        public async Task<ResultadoPaginado<UsuarioDto>> BuscarUsuariosAsync(
            string? usuario,
            string? proveedor,
            AgrupadorRol agrupador,
            int pagina = 1,
            int tamanoPagina = 10)
        {

            if (pagina <= 0) pagina = 1;
            if (tamanoPagina <= 0) tamanoPagina = 10;

            var query = _context.Usuarios
                .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
                .AsQueryable();

            switch (agrupador)
            {
                case AgrupadorRol.PROVEEDORES:
                    query = query.Where(u => u.UsuarioRoles.Any(ur => ur.Rol.Descripcion == "PROVEEDOR"));
                    //if (!string.IsNullOrWhiteSpace(proveedor))
                    //    query = query.Where(u => u.Proveedor.ClaveProveedor == proveedor);
                    break;

                case AgrupadorRol.ONEST:
                    query = query.Where(u => u.UsuarioRoles.Any(ur => ur.Rol.Descripcion == "ONEST"));
                    break;

                case AgrupadorRol.LOGISTICA:
                    query = query.Where(u => u.UsuarioRoles.Any(ur => ur.Rol.Descripcion == "LOGISTICA"));
                    break;

                default:
                    return new ResultadoPaginado<UsuarioDto>
                    {
                        PaginaActual = pagina,
                        TotalPaginas = 0,
                        TotalElementos = 0,
                        Elementos = new List<UsuarioDto>()
                    };
            }

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                query = query.Where(u =>
                    u.CorreoElectronico.Contains(usuario) ||
                    u.Nombre.Contains(usuario));
            }

            var totalElementos = await query.CountAsync();

            var totalPaginas = (int)Math.Ceiling(totalElementos / (double)tamanoPagina);

            var usuarios = await query
                .OrderByDescending(u => u.IdUsuario)
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .Select(u => new UsuarioDto
                {
                    Id = u.IdUsuario,
                    Email = u.CorreoElectronico,
                    NombreCompleto = u.Nombre,
                    Rol = u.UsuarioRoles.Select(ur => ur.Rol.Descripcion).FirstOrDefault(),
                    Activo = u.Estatus,
                    Habilitado = u.Estatus,
                })
                .ToListAsync();

            return new ResultadoPaginado<UsuarioDto>
            {
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalElementos = totalElementos,
                Elementos = usuarios
            };
        }

    }
}

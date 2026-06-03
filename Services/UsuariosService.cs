using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;
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
        ADMIN
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
                .Include(r => r.UsuarioRoles)
                .ThenInclude(r => r.Rol)
                .AsNoTracking()
                .Where(u => u.IdUsuario == id)
                .Select(u => new UsuarioDto
                {
                    Id = u.IdUsuario,
                    Email = u.CorreoElectronico,
                    Nombre = u.Nombre,
                    ApellidoPaterno = u.ApellidoPaterno,
                    ApellidoMaterno = u.ApellidoMaterno,
                    Activo = u.Estatus,
                    Habilitado = u.Estatus,
                    RfcProveedor = u.RfcProveedor,
                    Rol = u.UsuarioRoles.FirstOrDefault()!.Rol.Descripcion
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

        public async Task<ResultadoPaginado<UsuarioEmpresasDto>> BuscarUsuariosAsync(
            string? usuario,
            string? proveedor,
            AgrupadorRol? agrupador,
            int pagina = 1,
            int tamanoPagina = 10)
        {

            if (pagina <= 0) pagina = 1;
            if (tamanoPagina <= 0) tamanoPagina = 10;

            var query = _context.Usuarios
                .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
                .Include(u => u.UsuarioEmpresas)
                .AsQueryable();

            switch (agrupador)
            {
                case AgrupadorRol.PROVEEDORES:
                    query = query.Where(u => u.UsuarioRoles.Any(ur => ur.Rol.Descripcion == "PROVEEDOR"));
                    //if (!string.IsNullOrWhiteSpace(proveedor))
                    //    query = query.Where(u => u.Proveedor.ClaveProveedor == proveedor);
                    break;

                case AgrupadorRol.ADMIN:
                    query = query.Where(u => u.UsuarioRoles.Any(ur => ur.Rol.Descripcion == "ADMIN"));
                    break;

                default:
                    query = query.Where(u => u.UsuarioRoles.Any());
                    break;
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
                .Select(u => new UsuarioEmpresasDto
                {
                    Id = u.IdUsuario,
                    Email = u.CorreoElectronico,
                    Nombre = u.Nombre,
                    ApellidoPaterno = u.ApellidoPaterno,
                    ApellidoMaterno = u.ApellidoMaterno,
                    Rol = u.UsuarioRoles.Select(ur => ur.Rol.Descripcion).FirstOrDefault(),
                    Activo = u.Estatus,
                    Habilitado = u.Estatus,
                    Usuario = u.usuario,
                    Empresas = u.UsuarioEmpresas.Select(ue => ue.IdEmpresa).ToArray(),
                    RfcProveedor = u.RfcProveedor
                })
                .ToListAsync();

            return new ResultadoPaginado<UsuarioEmpresasDto>
            {
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalElementos = totalElementos,
                Elementos = usuarios
            };
        }

        public async Task<ApiResponseDto<bool>> AsociarEmpresasAsync(AsociarEmpresasRequestDto asociarEmpresas)
        {
            try
            {
                var usuario = await ObtenerUsuarioPorIdAsync(asociarEmpresas.IdUsuario); // _context.Usuarios.AnyAsync(u => u.IdUsuario == asociarEmpresas.IdUsuario);

                if (usuario is null)
                {
                    return new ApiResponseDto<bool>()
                    {
                        Success = false,
                        Message = "No se localizo el usuario, verifique el idUsuario",
                        StatusCode = System.Net.HttpStatusCode.NotFound,
                        Data = false
                    };
                }

                var empresas = await _context.Empresa.Where(e => asociarEmpresas.IdEmpresas.Contains(e.IdEmpresa)).ToListAsync();

                if (empresas.Count != asociarEmpresas.IdEmpresas.Length)
                {
                    return new ApiResponseDto<bool>()
                    {
                        Success = false,
                        Message = "No se localizaron todas las empresas, verifique los idEmpresas",
                        StatusCode = System.Net.HttpStatusCode.NotFound,
                        Data = false
                    };
                }

                var usuarioEmpresasExistentes = await _context.UsuarioEmpresa.Where(ue => ue.IdUsuario == asociarEmpresas.IdUsuario).ToListAsync();

                _context.UsuarioEmpresa.RemoveRange(usuarioEmpresasExistentes);
                var nuevasAsociaciones = asociarEmpresas.IdEmpresas.Select(idEmpresa => new UsuarioEmpresa
                {
                    IdUsuario = asociarEmpresas.IdUsuario,
                    IdEmpresa = idEmpresa
                });
                await _context.UsuarioEmpresa.AddRangeAsync(nuevasAsociaciones);
                await _context.SaveChangesAsync();

                //Se asocian las empresas al proveedor en caso de que el usuario sea proveedor
                if(usuario.Rol == "PROVEEDOR")
                {
                    var proveedor = await _context.Proveedores.FirstOrDefaultAsync(p => p.Rfc == usuario.RfcProveedor);
                    var proveedorEmpresasExistentes = await _context.ProveedorEmpresa.Where(pe => pe.IdProveedor == proveedor.Id_proveedor).ToListAsync();
                    _context.ProveedorEmpresa.RemoveRange(proveedorEmpresasExistentes);
                    if (proveedor is not null)
                    {
                        proveedor.ProveedorEmpresa = nuevasAsociaciones.Select(na => new ProveedorEmpresa
                        {
                            IdProveedor = proveedor.Id_proveedor,
                            IdEmpresa = na.IdEmpresa
                        }).ToList();
                        _context.Proveedores.Update(proveedor);
                        await _context.SaveChangesAsync();
                    }
                }

                return new ApiResponseDto<bool>()
                {
                    Success = true,
                    Message = "Empresas asociadas correctamente al usuario",
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Data = true
                };
            }
            catch (Exception ex)
            {

                throw new Exception("Error al asociar empresas al usuario verifique", ex);
            }
        }

    }
}

using ApiProveedores.Dto.Auth;
using ApiProveedores.Helper;
using ApiProveedores.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Google.Rpc.Context.AttributeContext.Types;

namespace ApiProveedores.Services
{
    public class TokenService
    {
        public const int REFRESH_TOKEN_DIAS_VALIDOS = 7;
        private readonly PortalDbContext _context;
        private readonly IConfiguration _config;
        private readonly ProveedoresService _proveedoresService;
        private readonly AuthService _authService;
        public TokenService(PortalDbContext context, IConfiguration config, 
            ProveedoresService proveedoresService, AuthService authService)
        {
            _proveedoresService = proveedoresService;
            _context = context;
            _config = config;
            _authService = authService;
        }

        public async Task<string> GenerarJwt(Usuario usuario)
        {
            //var user = await _authService.LoginAsync(usuario.CorreoElectronico, usuario.Password);
            var roleClaim = usuario.UsuarioRoles?.Select(ur => ur.Rol?.Descripcion).FirstOrDefault() ?? "ANONIMO";
            var roles = usuario.UsuarioRoles?
                .Select(ur => ur.Rol?.Descripcion)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct()
                .ToList() ?? new List<string> { "ANONIMO" };

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
                new Claim("email", usuario.CorreoElectronico),
                new Claim("name", usuario.CorreoElectronico),
                new Claim("given_name", usuario.Nombre ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach(var rol in roles)
            {
                claims.Add(new Claim("role", rol));
            }

           var proveedores = await (
                from ue in _context.UsuarioEmpresa
                join pe in _context.ProveedorEmpresa on ue.IdEmpresa equals pe.IdEmpresa
                join p in _context.Proveedores on pe.IdProveedor equals p.Id_proveedor
                where ue.IdUsuario == usuario.IdUsuario
                select new { p.Id_proveedor, p.Nombre}
                ).Distinct().ToListAsync();

            if ( proveedores.Any())
            {
                foreach (var prov in proveedores)
                {
                    claims.Add(new Claim(ClaimTypes.GroupSid, prov.Id_proveedor.ToString()));
                    claims.Add(new Claim("cveprov", prov.Nombre));
                }
            }

            var envSecret = Environment.GetEnvironmentVariable("PROVEEDORES_API_CORE_JWT_SECRET_KEY");
            if (string.IsNullOrWhiteSpace(envSecret))
            {
                envSecret = string.Empty;
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(envSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: TimeHelper.UtcNow().AddMinutes(int.Parse(_config["JwtSettings:ExpirationMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerarRefreshTokenAsync(Usuario usuario)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refresh = new RefreshToken
            {
                UsuarioId = usuario.IdUsuario,
                Token = token,
                ExpiraEn = TimeHelper.UtcNow().AddDays(REFRESH_TOKEN_DIAS_VALIDOS),
                CreadoEn = TimeHelper.UtcNow()
            };

            _context.RefreshTokens.Add(refresh);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<(string jwt, string refreshToken)> RenovarAsync(string refreshToken)
        {
            // Usar una sola referencia a la hora actual en UTC para evitar inconsistencias
            var ahoraUtc = TimeHelper.UtcNow();

            var tokenActual = await _context.RefreshTokens
                .Include(t => t.Usuario)
                .FirstOrDefaultAsync(t =>
                    t.Token == refreshToken &&
                    t.RevocadoEn == null &&
                    t.ExpiraEn > ahoraUtc);

            if (tokenActual == null)
                throw new SecurityTokenException("Refresh token inválido o expirado");

            // Marcar el token actual como revocado
            tokenActual.RevocadoEn = ahoraUtc;

            // Generar nuevos tokens
            if (!tokenActual.Usuario.UsuarioRoles.Any()){
                var user = await _context.Usuarios
                .Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol)
                .FirstOrDefaultAsync(u => u.CorreoElectronico == tokenActual.Usuario.CorreoElectronico);

                tokenActual.Usuario.UsuarioRoles = user.UsuarioRoles;
            }

            var nuevoJwt = await GenerarJwt(tokenActual.Usuario);
            var nuevoRefresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var refreshNuevo = new RefreshToken
            {
                UsuarioId = tokenActual.Usuario.IdUsuario,
                Token = nuevoRefresh,
                ExpiraEn = ahoraUtc.AddDays(REFRESH_TOKEN_DIAS_VALIDOS),
                CreadoEn = ahoraUtc,
                ReemplazadoPor = tokenActual.Token
            };

            _context.RefreshTokens.Add(refreshNuevo);
            await _context.SaveChangesAsync();

            return (nuevoJwt, nuevoRefresh);
        }
    }
}

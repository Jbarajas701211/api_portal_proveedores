using ApiProveedores.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using ApiProveedores.Dto.Auth;
using System.Collections.Generic;
using System.Linq;
using ApiProveedores.Helper;

namespace ApiProveedores.Services
{
    public class TokenService
    {
        public const int REFRESH_TOKEN_DIAS_VALIDOS = 7;
        private readonly PortalDbContext _context;
        private readonly IConfiguration _config;
        private readonly ProveedoresService _proveedoresService;
        public TokenService(PortalDbContext context, IConfiguration config, 
            ProveedoresService proveedoresService)
        {
            _proveedoresService = proveedoresService;
            _context = context;
            _config = config;
        }

        public async Task<string> GenerarJwt(Usuario usuario)
        {
            var roleClaim = usuario.UsuarioRoles?.Select(ur => ur.Rol?.Descripcion).FirstOrDefault() ?? "ANONIMO";
            var roles = usuario.UsuarioRoles?
                .Select(ur => ur.Rol?.Descripcion)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct()
                .ToList() ?? new List<string> { "ANONIMO" };

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.CorreoElectronico),
                new Claim(ClaimTypes.GivenName, usuario.Nombre ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach(var rol in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, rol));
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

            var envSecret = Environment.GetEnvironmentVariable("CITAS_API_CORE_JWT_SECRET_KEY");
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
            var tokenActual = await _context.RefreshTokens
                .Include(t => t.Usuario)
                .FirstOrDefaultAsync(t =>
                    t.Token == refreshToken &&
                    t.RevocadoEn == null &&
                    t.ExpiraEn > TimeHelper.NowMexicoUnspecified());

            if (tokenActual == null)
                throw new SecurityTokenException("Refresh token inválido o expirado");

            // Marcar el token actual como revocado
            tokenActual.RevocadoEn = TimeHelper.NowMexicoUnspecified();

            // Generar nuevos tokens
            var nuevoJwt = await GenerarJwt(tokenActual.Usuario);
            var nuevoRefresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var refreshNuevo = new RefreshToken
            {
                UsuarioId = tokenActual.Usuario.IdUsuario,
                Token = nuevoRefresh,
                ExpiraEn = TimeHelper.NowMexicoUnspecified().AddDays(7),
                CreadoEn = TimeHelper.NowMexicoUnspecified(),
                ReemplazadoPor = tokenActual.Token
            };

            _context.RefreshTokens.Add(refreshNuevo);
            await _context.SaveChangesAsync();

            return (nuevoJwt, nuevoRefresh);
        }
    }
}

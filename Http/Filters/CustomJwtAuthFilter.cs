namespace ApiProveedores.Http.Filters
{
    using ApiProveedores.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Text;
    using System.Threading.Tasks;

    public class CustomJwtAuthFilter : IAsyncAuthorizationFilter
    {
        private readonly JwtSettings _jwtSettings;

        public CustomJwtAuthFilter(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var headers = context.HttpContext.Request.Headers;

            if (!headers.TryGetValue("X-User-Token", out var tokenValue) || string.IsNullOrWhiteSpace(tokenValue))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            var envSecret = Environment.GetEnvironmentVariable("CITAS_API_CORE_JWT_SECRET_KEY");
            _jwtSettings.SecretKey = envSecret;

            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(tokenValue, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role
                }, out _);

                context.HttpContext.User = principal;
            }
            catch
            {
                context.Result = new UnauthorizedResult();
            }

            await Task.CompletedTask;
        }
    }

}

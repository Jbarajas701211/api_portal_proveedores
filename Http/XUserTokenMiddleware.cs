using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;

namespace ApiProveedores.Http
{
    public class XUserTokenMiddleware
    {
        private readonly RequestDelegate _next;

        public XUserTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-User-Token", out var userToken) &&
                !string.IsNullOrWhiteSpace(userToken))
            {
                context.Request.Headers["Authorization"] = $"Bearer {userToken}";
            }

            await _next(context);
        }
    }

}

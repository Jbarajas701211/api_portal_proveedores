namespace ApiProveedores.Http.Filters
{
    using ApiProveedores.Services.Exceptions;
    using Grpc.Core;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Logging;
    using System.IO;
    using System.Security.Claims;
    using System.Text.Json;

    public class GlobalExceptionHandler : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        private string ReadBody(HttpContext context)
        {
            if (context.Request.ContentLength == null || context.Request.ContentLength == 0)
                return "N/A";

            context.Request.EnableBuffering();

            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = reader.ReadToEnd();
            context.Request.Body.Position = 0;

            try
            {
                using var jsonDoc = JsonDocument.Parse(body);
                return JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                {
                    WriteIndented = false
                });
            }
            catch
            {
                return body.Replace("\n", "").Replace("\r", "").Replace(" ", "");
            }
        }


        public void OnException(ExceptionContext context)
        {
            var ex = context.Exception;

            if (ex is ApiProveedoresException appEx)
            {

                var user = context.HttpContext.User;
                var userName = user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value ?? "an�nimo";
                var nombreCompleto = user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?.Value;
                var rol = user.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? "SIN ROL";
                var ruta = context.HttpContext.Request.Path;
                var metodo = context.HttpContext.Request.Method;
                var queryParams = context.HttpContext.Request.QueryString.HasValue
                    ? context.HttpContext.Request.QueryString.Value
                    : "N/A";

                string bodyContent = ReadBody(context.HttpContext);

                _logger.LogWarning(
                    "\n[Api Citas - Trazabilidad]\n\tM�todo: {Metodo}\n\tRuta: {Ruta}\n\tUsuario: {Usuario} ({NombreCompleto})\n\tRol: {Rol}\n\tQuery: {Query}\n\tBody: {Body}\n\tMensaje: {Mensaje}\n",
                    metodo,
                    ruta,
                    userName,
                    nombreCompleto ?? "N/D",
                    rol,
                    queryParams,
                    bodyContent,
                    appEx.Message
                );


                context.Result = new ObjectResult(new { message = appEx.Message })
                {
                    StatusCode = 400
                };
            }
            else
            {
                _logger.LogError(ex,
                    "Error inesperado en {Ruta}. Usuario: {Usuario}",
                    context.HttpContext.Request.Path,
                    context.HttpContext.User.Identity?.Name ?? "an�nimo");

                context.Result = new ObjectResult(new { 
                    mensaje = "Ocurri� un error interno inesperado.",
                })
                {
                    StatusCode = 500
                };
            }

            context.ExceptionHandled = true;
        }
    }


}

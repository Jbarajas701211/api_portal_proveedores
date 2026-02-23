using ApiProveedores.Models;
using static AuthService;
using System.Threading.Tasks;
using ApiProveedores.Helper;
using ApiProveedores.Services.PubSub;
using System;

namespace ApiProveedores.Services.Helper
{
    public class HelperTraceService
    {

        private readonly PortalDbContext _context;
        
        public HelperTraceService(PortalDbContext context)
        {
            _context = context;
        }

        public async Task SaveTraceUsuarios(int idUsuario, EventoUsuario evento, string descripcion = "")
        {
            var trace = new TraceUsuario()
            {
                Evento = evento,
                IdUsuario = idUsuario,
                Descripcion = descripcion,
                RegistradoEn = DateTimeOffset.UtcNow,
            };
            _context.TraceUsuarios.Add(trace);
            await _context.SaveChangesAsync();
        }

    }
}

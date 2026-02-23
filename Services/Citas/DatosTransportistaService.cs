using ApiProveedores.Dto.Entrada;
using ApiProveedores.Models;
using ApiProveedores.Services.Citas.Validators;
using ApiProveedores.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProveedores.Services.Citas
{
    public class DatosTransportistaService
    {
        private readonly PortalDbContext _context;
        private readonly ActualizarDatosTransportistaCitaValidator _validator;

        public DatosTransportistaService(PortalDbContext context, ActualizarDatosTransportistaCitaValidator validator)
        {
            _context = context;
            _validator = validator;
        }


        public async Task<Cita> ActualizarDatosTransporteAsync(
            long citaId,
            long proveedorId,
            TransporteDto dto,
            CancellationToken ct = default)
        {

            // Secuencia de validaciones.
            var ctx = new ActualizarDatosTransportisCitaContext { IdCita = citaId, ProveedorId = proveedorId, Dto = dto };
            var result = await _validator.ValidateAsync(ctx, ct);
            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            var cita = ctx.State.cita;

            if (dto.NombreChofer is not null) cita.NombreChofer = dto.NombreChofer.Trim().ToUpperInvariant();
            if (dto.NombreAyudante is not null) cita.NombreAyudante = dto.NombreAyudante?.Trim().ToUpperInvariant();
            if (dto.TipoUnidad is not null) cita.TipoUnidad = dto.TipoUnidad?.Trim().ToUpperInvariant();
            if (dto.Placas is not null) cita.Placas = dto.Placas?.Trim().ToUpperInvariant();
            if (dto.LineaTransportista is not null) cita.LineaTransportista = dto.LineaTransportista?.Trim().ToUpperInvariant();
            if (dto.Observaciones is not null) cita.Observaciones = dto.Observaciones?.Trim().ToUpperInvariant();

            await _context.SaveChangesAsync(ct);
            return cita;
        }

    }
}

using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Mappers;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Helper;
using ApiProveedores.Models;
using ApiProveedores.Services.Citas.Validators;
using ApiProveedores.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProveedores.Services.Citas
{
    public class DetalleCitaService
    {
        private readonly PortalDbContext _context;
        private readonly RegistroDetalleCitaValidator _registroDetalleCitaValidator;
        private readonly ActualizarDetalleCitaValidator _actualizarDetalleCitaValidator;
        private readonly EliminarDetalleCitaValidator _eliminarDetalleCitaValidator;

        public DetalleCitaService(
            PortalDbContext context, 
            RegistroDetalleCitaValidator registroDetalleCitaValidator, 
            ActualizarDetalleCitaValidator actualizarDetalleCitaValidator,
            EliminarDetalleCitaValidator eliminarDetalleCitaValidator)
        {
            _context = context;
            _registroDetalleCitaValidator = registroDetalleCitaValidator;
            _actualizarDetalleCitaValidator = actualizarDetalleCitaValidator;
            _eliminarDetalleCitaValidator = eliminarDetalleCitaValidator;
        }

        public async Task<bool> ExisteOcEnCitaAsync(long citaId, string oc, CancellationToken ct = default)
        {
            oc = (oc ?? string.Empty).Trim();
            return await _context.CitasDetalle
                .AsNoTracking()
                .AnyAsync(d => d.CitaId == citaId && d.Oc == oc, ct);
        }

        public async Task<IReadOnlyList<CitaDetalleDto>> ObtenerDetallesPorCitaAsync(
            long citaId,
            long proveedorId,
            CancellationToken ct = default)
        {
            var existeCita = await _context.Citas
                .AsNoTracking()
                .AnyAsync(c => c.Id == citaId && c.ProveedorId == proveedorId, ct);

            if (!existeCita)
                return new List<CitaDetalleDto>(0);

            var detalles = await _context.CitasDetalle
                .AsNoTracking()
                .Where(d => d.CitaId == citaId && d.Cita.ProveedorId == proveedorId)
                .OrderBy(d => d.Oc)
                .ToListAsync(ct);

            return detalles.ToDtoList();
        }

        public async Task<IReadOnlyList<CitaDetalleDto>> ObtenerDetallesPorCitaAsync(
            long citaId,
            CancellationToken ct = default)
        {
            var existeCita = await _context.Citas
                .AsNoTracking()
                .AnyAsync(c => c.Id == citaId, ct);

            if (!existeCita)
                return new List<CitaDetalleDto>(0);

            var detalles = await _context.CitasDetalle
                .AsNoTracking()
                .Where(d => d.CitaId == citaId)
                .OrderBy(d => d.Oc)
                .ToListAsync(ct);

            return detalles.ToDtoList();
        }

        public async Task<CitaDetalle> RegistrarDetalleCitaAsync(CrearCitaDetalleDto crearDetalleCitaDto, long idProveedor, CancellationToken ct = default)
        {

            var context = new RegistrarDetalleCitaContext { Dto = crearDetalleCitaDto, ProveedorId = idProveedor };

            // Ejecuta secuencia de validaciones para el registro de la cita. 
            var result = await _registroDetalleCitaValidator.ValidateAsync(context, ct);
            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            // Crea detalle de la cita. 
            var cita = context.State.cita;
            var detalle = new CitaDetalle
            {
                CitaId = crearDetalleCitaDto.CitaId,
                Oc = crearDetalleCitaDto.Oc.Trim(),
                Origen = context.State.orden.Origen,
                ClaveAlmacen = context.State.orden.CveAlmacen,
                FechaVencimiento = context.State.orden.Fechavenci,
                CantidadTotal = (int) context.State.orden.Cantitotal,
                CantidadPorCita = crearDetalleCitaDto.CantidadPorCita,
                RegistradoEn = TimeHelper.NowMexicoUnspecified()
            };
            _context.CitasDetalle.Add(detalle);

            // Crea detalle de la cita. 
            await _context.SaveChangesAsync(ct);

            return detalle;
        }


        public async Task<CitaDetalle> ActualizarDetalleCitaAsync(ActualizarCitaDetalleDto actualizarDetalleCitaDto, long idProveedor, CancellationToken ct = default)
        {

            var context = new ActualizarDetalleCitaContext { IdCita = actualizarDetalleCitaDto.CitaId, Dto = actualizarDetalleCitaDto, ProveedorId = idProveedor };

            // Ejecuta secuencia de validaciones para actualizar el detalle de la cita. 
            var result = await _actualizarDetalleCitaValidator.ValidateAsync(context, ct);
            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            // Actualiza detalle de la cita. 
            await _context.CitasDetalle
                .Where(c => c.CitaId == actualizarDetalleCitaDto.CitaId && c.Cita.ProveedorId == idProveedor)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.CantidadPorCita, _ => actualizarDetalleCitaDto.CantidadPorCita));


            await _context.SaveChangesAsync(ct);

            var detalle = await _context.CitasDetalle
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CitaId == actualizarDetalleCitaDto.CitaId
                                       && c.Cita.ProveedorId == idProveedor, ct);
            return detalle;
        }

        public async Task EliminarDetalleCitaAsync(EliminaCitaDetalleDto actualizarDetalleCitaDto, long idProveedor, CancellationToken ct = default)
        {

            var context = new EliminarDetalleCitaContext { IdCita = actualizarDetalleCitaDto.CitaId, Dto = actualizarDetalleCitaDto, ProveedorId = idProveedor };

            // Ejecuta secuencia de validaciones para actualizar el detalle de la cita. 
            var result = await _eliminarDetalleCitaValidator.ValidateAsync(context, ct);
            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            // Elimina detalle de la cita. 
            
            _context.Remove(context.State.citaDetalle);
            await _context.SaveChangesAsync(ct);
        }
    }
}

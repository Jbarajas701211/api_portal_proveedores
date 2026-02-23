using ApiProveedores.Dto.Entrada;
using ApiProveedores.Helper;
using ApiProveedores.Models;
using ApiProveedores.Services.Citas.Validators;
using ApiProveedores.Services.Exceptions;
using Google.Api;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProveedores.Services.Citas
{
    public class EntregaService
    {
        private readonly PortalDbContext _context;
        private readonly EntregaValidator _validator;
        private readonly ActualizaResumenService _actualizaResumenService;
        private readonly CantidadesTeoricasService _cantidadesTeoricasService;
        private readonly HelperOrdenService _helperOrdenService;

        public EntregaService(PortalDbContext context, 
            EntregaValidator validator, 
            ActualizaResumenService actualizaResumenService,
            CantidadesTeoricasService cantidadesTeoricasService,
            HelperOrdenService helperOrdenService)
        {
            _context = context;
            _validator = validator;
            _actualizaResumenService = actualizaResumenService;
            _cantidadesTeoricasService = cantidadesTeoricasService;
            _helperOrdenService = helperOrdenService;
        }

        public async Task<IReadOnlyList<CitaEntrega>> RegistraFallaMasivaAsync(
            long[] idsCita, string notas, long userId, CancellationToken ct = default)
        {
            if (idsCita is null || idsCita.Length == 0)
                return Array.Empty<CitaEntrega>();

            var ahoraUtc = DateTime.UtcNow;
            var fechaHoy = DateOnly.FromDateTime(ahoraUtc);
            var horaAhora = TimeOnly.FromDateTime(ahoraUtc);

            var resultados = new List<CitaEntrega>(idsCita.Length);

            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            foreach (var id in idsCita.Distinct())
            {
                var dto = new EntregaDto
                {
                    CitaId = id,
                    FechaEntrega = fechaHoy,
                    HoraRecepcion = horaAhora,
                    CantidadEntregada = 0,
                    Estatus = EntregaEstatus.FALLO,
                    Notas = notas
                };

                var entrega = await RegistraEntregaAsync(dto, userId, ct);
                resultados.Add(entrega);
            }
            await tx.CommitAsync(ct);
            return resultados;
        }

        private async Task MarcarSeguimientoConIncidenciasAsync(long citaId, CancellationToken ct = default)
        {
            var seguimiento = await _context.CitasSeguimiento
                .Where(s => s.CitaId == citaId && s.EstadoActivo)
                .OrderByDescending(s => s.RegistradoEn)
                .FirstOrDefaultAsync(ct);

            if (seguimiento is null)
                return;

            if (!seguimiento.ConIncidencias)
            {
                seguimiento.ConIncidencias = true;
                await MarcarSeguimientoConIncidenciasAsync(citaId);
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task<CitaEntrega> RegistraEntregaAsync(
            EntregaDto dto,
            long usuarioId,
            CancellationToken ct = default)
        {
            // validaciones
            var ctx = new EntregaValidatorContext { IdCita = dto.CitaId, Dto = dto };
            var result = await _validator.ValidateAsync(ctx, ct);
            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            // abre transaccion
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var entrega = new CitaEntrega
                {
                    CitaId = dto.CitaId,
                    FechaEntrega = dto.FechaEntrega,
                    HoraRecepcion = dto.HoraRecepcion,
                    CantidadEntregada = dto.CantidadEntregada,
                    Estatus = dto.Estatus,
                    Anden = dto.Anden,
                    Acuse = dto.Acuse,
                    Notas = !string.IsNullOrEmpty(dto.Notas) ? dto.Notas.ToUpperInvariant() : string.Empty,
                    RegistradoEn = TimeHelper.NowMexicoUnspecified()
                };

                _context.Add(entrega);

                if (entrega.Estatus == EntregaEstatus.ENTREGO)
                {
                    // cambio de estado a ENTREGADA
                    await _context.Citas
                        .Where(c => c.Id == dto.CitaId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(c => c.Estado, _ => EstadoCita.ENTREGADA.ToString()),
                            ct);

                    // actualiza el resumen
                    await _actualizaResumenService.ActualizaResumen(ctx.State.cita, EstadoCita.ENTREGADA);


                    // marca seguimiento con incidencias y bloquea ordenes
                    await MarcarSeguimientoConIncidenciasAsync(dto.CitaId);

                    // bloque ordenes del detalle, para procesamiento 
                    await _helperOrdenService.TraceOrdenesDeProcesamientoAsync(
                        ctx.State.cita, usuarioId,
                        $"Orden bloqueada para ajustar las capacidades (desde SAP) y las cantidades reales, para la cita: {ctx.State.cita.Id}",
                        TipoOperacion.BLOQUEADA,
                        ct);
                }
                else 
                {
                    // cambio de estado a FALLO
                    await _context.Citas
                        .Where(c => c.Id == dto.CitaId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(c => c.Estado, _ => EstadoCita.FALLO.ToString()),
                            ct);

                    // actualiza el resumen
                    await _actualizaResumenService.ActualizaResumen(ctx.State.cita, EstadoCita.FALLO);
                    
                    // bloque ordenes del detalle, para procesamiento 
                    await _helperOrdenService.TraceOrdenesDeProcesamientoAsync(
                        ctx.State.cita, 
                        usuarioId,
                        $"Orden bloqueada para cancelar la Entrega/Entrante en SAP (por motivo de FALLO), para la cita: {ctx.State.cita.Id}",
                        TipoOperacion.BLOQUEADA,
                        ct);
                }
                await _context.SaveChangesAsync(ct);



                // commit final
                await tx.CommitAsync(ct);

                return entrega;
            }
            catch
            {
                // rollback
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<List<CitaEntrega>> ObtenerEntregasPorCitaAsync(
            long citaId,
            CancellationToken ct = default)
        {
            return await _context.CitasEntregas
                .Where(e => e.CitaId == citaId)
                .OrderBy(e => e.FechaEntrega)
                .ThenBy(e => e.HoraRecepcion)
                .ToListAsync(ct);
        }

    }
}

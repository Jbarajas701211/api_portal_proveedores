namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;
    using System.Linq;
    using System;

    public sealed class CancelacionValidator : AbstractValidator<CancelacionContext>
    {
        private readonly PortalDbContext _db;
        public CancelacionValidator(PortalDbContext db, ParametroSistemaService parametroSistemaService)
        {
            _db = db;

            // validar que el id de la cita sea mayor que cero
            RuleFor(x => x.IdCita)
                .GreaterThan(0)
                .WithMessage("El id de la cita debe ser mayor a cero.")
                .DependentRules(() => {

                    // validar que la cita exista
                    RuleFor(x => x.IdCita)
                        .MustAsync(async (ctx, idCita, ct) =>
                        {
                            var cita = await _db.Citas
                                .Include(c => c.Detalles)
                                .Include(c => c.Proveedor)
                                .FirstOrDefaultAsync(cita => cita.Id == idCita && cita.ProveedorId == ctx.ProveedorId);
                            ctx.State.cita = cita;
                            return cita != null;
                        })
                        .WithMessage(x => $"La cita con id '{x.IdCita}' no existe.")
                        .DependentRules(() =>
                        {

                            // Si existe la cita valida su estado
                            RuleFor(x => x)
                                .Must(x => {
                                    return x.State.cita.Estado == EstadoCita.AGENDADA.ToString();

                                })
                                .WithMessage(x =>
                                    $"La cita con ID '{x.IdCita}' solo puede cambiar a CANCELADA si está en estado AGENDADA. Estado actual: '{x.State.cita!.Estado}'.");


                            RuleFor(x => x.State.cita)
                                .MustAsync(async (model, cita, ct) =>
                                {
                                    if (cita == null || cita.Detalles == null || !cita.Detalles.Any())
                                        return true;

                                    var ordenes = cita.Detalles
                                        .Select(d => d.Oc)
                                        .Where(oc => !string.IsNullOrWhiteSpace(oc))
                                        .Select(oc => oc.Trim())
                                        .Distinct()
                                        .ToList();

                                    if (!ordenes.Any())
                                        return true;

                                    var ultimos = await _db.OrdenesSeguimiento
                                        .AsNoTracking()
                                        .Where(o => ordenes.Contains(o.Nopedido) && o.EstadoActivo)
                                        .GroupBy(o => o.Nopedido)
                                        .Select(g => g.OrderByDescending(o => o.RegistradoEn).FirstOrDefault())
                                        .ToListAsync(ct);

                                    var bloqueadas = ultimos
                                        .Where(ultimo =>
                                            ultimo != null &&
                                            string.Equals(ultimo.Evento, "BLOQUEADA", StringComparison.OrdinalIgnoreCase))
                                        .Select(ultimo => ultimo!.Nopedido)
                                        .ToList();

                                    model.State.OrdenesBloqueadas = bloqueadas;

                                    return !bloqueadas.Any();
                                })
                                .WithMessage(ctx =>
                                {
                                    var bloqueadas = ctx.State.OrdenesBloqueadas;

                                    if (bloqueadas == null || !bloqueadas.Any())
                                        return "Alguna orden de compra asociada a la cita se encuentra BLOQUEADA para su procesamiento.";

                                    if (bloqueadas.Count == 1)
                                        return $"La orden de compra {bloqueadas[0]} se encuentra BLOQUEADA para su procesamiento.";

                                    var joined = string.Join(", ", bloqueadas);
                                    return $"Las órdenes de compra {joined} se encuentran BLOQUEADAS para su procesamiento.";
                                });

                        });
                });


        }
    }

}

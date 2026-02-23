namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Linq;

    public sealed class RegistroDetalleCitaValidator : AbstractValidator<RegistrarDetalleCitaContext>
    {
        private readonly PortalDbContext _db;

        public RegistroDetalleCitaValidator(PortalDbContext db)
        {
            _db = db;

            // validar que el id de la cita sea mayor que cero
            RuleFor(x => x.Dto.CitaId)
                .GreaterThan(0)
                .WithMessage("El id de la cita debe ser mayor a cero.")
                .DependentRules(() => {

                    // validar que la cita exista
                    RuleFor(x => x.Dto.CitaId)
                        .MustAsync(async (ctx, idCita, ct) =>
                        {
                            var cita = await _db.Citas
                                .Include(c => c.Detalles)
                                .FirstOrDefaultAsync(cita => cita.Id == idCita && cita.ProveedorId == ctx.ProveedorId);
                            ctx.State.cita = cita;
                            return cita != null;
                        })
                        .WithMessage(x => $"La cita con id '{x.Dto.CitaId}' no existe.")
                        .DependentRules(() =>
                        {

                            // Valida si el estado es correo para agregar detalle. 
                            RuleFor(x => x)
                                .Must(x => {
                                    return
                                        x.State.cita.Estado == EstadoCita.CREADA.ToString() ||
                                        x.State.cita.Estado == EstadoCita.REAGENDADA.ToString();
                                })
                                .WithMessage(x =>
                                    $"La cita con ID '{x.IdCita}' solo puede alterada si está en estado CREADA o REAGENDADA. Estado actual: '{x.State.cita!.Estado}'.");

                            // Si existe la cita valida su estado
                            RuleFor(x => x)
                                .Must(x => {
                                    return !(x.State.cita!.Estado != "CREADA");
                                })
                                .WithMessage(x =>
                                    $"No se puede registrar el detalle de la cita con ID '{x.Dto.CitaId}', por que su estado actual es '{x.State.cita!.Estado}'.");
                        });


                    // validar que orden de exista
                    RuleFor(x => x.Dto.Oc)
                        .NotEmpty()
                            .WithMessage("La OC no puede estar vacía.")
                        .Matches(@"^\d+$")
                            .WithMessage(x => $"La OC '{x.Dto.Oc}' no es válida, NO están permitidos caracteres especiales.")
                            .DependentRules(() => {
                                RuleFor(x => x.Dto.CitaId)
                                    .MustAsync(async (ctx, idCita, ct) =>
                                    {
                                        var orden = await _db.Ordenes
                                            .FirstOrDefaultAsync(cita => cita.Nopedido == ctx.Dto.Oc);
                                        ctx.State.orden = orden;
                                        return orden != null;
                                    })
                                    .WithMessage(x => $"La cita con id '{x.Dto.CitaId}' no existe.")
                                    .DependentRules(() =>
                                    {

                                        // Valida si la orden ya esta registrada en la cita.
                                        RuleFor(x => x)
                                            .MustAsync(async (ctx, ct) =>
                                            {

                                                ctx.Dto.Oc = (ctx.Dto.Oc ?? string.Empty).Trim();
                                                var result = await _db.CitasDetalle
                                                    .AsNoTracking()
                                                    .AnyAsync(d => d.CitaId == ctx.Dto.CitaId && d.Oc == ctx.Dto.Oc, ct);
                                                return !result;
                                            })
                                            .WithMessage(x =>
                                                $"La OC: '{x.Dto.Oc}' ya esta registrada en la cita '{x.Dto.CitaId}'.");


                                        RuleFor(x => x.State.orden)
                                            .MustAsync(async (model, orden, ct) =>
                                            {
                                                if (orden == null)
                                                    return false;

                                                // ultimo estado ACTIVO de esa orden
                                                var ultimo = await _db.OrdenesSeguimiento
                                                    .AsNoTracking()
                                                    .Where(o => o.Nopedido == orden.Nopedido && o.EstadoActivo)
                                                    .OrderByDescending(o => o.RegistradoEn)
                                                    .FirstOrDefaultAsync(ct);

                                                // si no tiene seguimiento, se permite
                                                if (ultimo == null)
                                                    return true;

                                                // solo NO procede si el ultimo evento activo es BLOQUEADO
                                                return !string.Equals(ultimo.Evento, "BLOQUEADA",
                                                    StringComparison.OrdinalIgnoreCase);
                                            })
                                            .WithMessage(ctx =>
                                            {
                                                var oc = ctx.State.orden?.Nopedido ?? "(sin orden)";
                                                return $"La orden de compra {oc} se encuentra BLOQUEADA y no puede agregarse.";
                                            });

                                        // Valida si la orden no esta vencida. 
                                        RuleFor(x => x.State.orden)
                                            .Must(orden =>
                                            {
                                                if (orden == null) return false;
                                                var vencUtcDate = DateTime.SpecifyKind(orden.Fechavenci, DateTimeKind.Utc).Date;
                                                var hoyUtcDate = DateTime.UtcNow.Date;
                                                return vencUtcDate >= hoyUtcDate;
                                            })
                                            .WithMessage(ctx =>
                                            {
                                                var orden = ctx.State.orden;
                                                return $"La orden de compra {orden.Nopedido} está vencida desde {orden.Fechavenci:yyyy-MM-dd}.";
                                            });

                                        // Si la orden ya esta completada (y no puede ser agregada al detalle de la cita)
                                        RuleFor(x => x.State.orden)
                                            .Must(orden =>
                                            {
                                                return orden.Status != 1;
                                            })
                                            .WithMessage(ctx =>
                                            {
                                                var orden = ctx.State.orden;
                                                return $"La orden de compra {orden.Nopedido} tiene estatus COMPLETADA, por tanto esta bloqueada para ser modificada.";
                                            });

                                        // Si la orden ya esta cancelada (y no puede ser agregada al detalle de la cita)
                                        RuleFor(x => x.State.orden)
                                            .Must(orden =>
                                            {
                                                return orden.Notacanc != 1;
                                            })
                                            .WithMessage(ctx =>
                                            {
                                                var orden = ctx.State.orden;
                                                return $"La orden de compra {orden.Nopedido} tiene estatus CANCELADA, por tanto esta bloqueada para ser modificada.";
                                            });


                                        // CantidadPorCita > 0
                                        RuleFor(x => x.Dto.CantidadPorCita)
                                            .GreaterThan(0)
                                            .WithMessage(ctx =>
                                                $"La orden de compra {ctx.State.orden.Nopedido} para cita, indica una cantidad ({ctx.Dto.CantidadPorCita}) inválida.");

                                        // CantidadPorCita <= CantidadTotal de la orden
                                        RuleFor(x => x)
                                            .MustAsync(async (ctx, _, ct) =>
                                            {
                                                var ordenTeorica = await _db.OrdenCantidadTeorica
                                                    .AsNoTracking()
                                                    .FirstOrDefaultAsync(o => o.Oc == ctx.State.orden.Nopedido, ct);

                                                // Si no existe el registro, usamos Cantitotal de la orden del estado
                                                int disponible = (ordenTeorica == null)
                                                    ? (int) ctx.State.orden.Cantitotal
                                                    : Math.Max(0,
                                                        ordenTeorica.CantidadTotal - ordenTeorica.CantidadTeorica);
                                                ctx.State.cantidadDisponibleOC = disponible;
                                                return ctx.Dto.CantidadPorCita <= disponible;
                                            })
                                            .WithMessage(x =>
                                                $"La orden de compra {x.State.orden.Nopedido} para cita, indica una cantidad ({x.Dto.CantidadPorCita}) que rebasa el número de piezas disponibles ({x.State.cantidadDisponibleOC}).");

                                    });
                            });
                });


        }
    }

}

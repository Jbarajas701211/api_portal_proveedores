namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;
    using System.Linq;

    public sealed class EliminarDetalleCitaValidator : AbstractValidator<EliminarDetalleCitaContext>
    {
        private readonly PortalDbContext _db;

        public EliminarDetalleCitaValidator(PortalDbContext db)
        {
            _db = db;

            // validar que el id de la cita sea mayor que cero
            RuleFor(x => x.Dto.CitaId)
                .GreaterThan(0)
                .WithMessage("El id de la cita debe ser mayor a cero.")
                .DependentRules(() => {


                    // validar que el detalle de la cita exista
                    RuleFor(x => x.Dto.CitaId)
                        .MustAsync(async (ctx, idCita, ct) =>
                        {
                            var citaDetalle = await _db.CitasDetalle
                                .FirstOrDefaultAsync(cita => cita.CitaId == idCita && cita.Cita.ProveedorId == ctx.ProveedorId && cita.Oc == ctx.Dto.Oc);
                            ctx.State.citaDetalle = citaDetalle;
                            return citaDetalle != null;
                        })
                        .WithMessage(x => $"El detalle de cita indicado id cita '{x.Dto.CitaId}' y orden: '{x.Dto.Oc}' no existe.")
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

                                    // Valida si el estado es correcto para eliminar detalle. 
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
                                            $"No se puede eliminar el detalle de la cita con ID '{x.Dto.CitaId}', por que su estado actual es '{x.State.cita!.Estado}'.");
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

                                                // Si la orden ya esta completada (no puede ser eliminado el detalle de la cita)
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

                                            });
                                    });
                        });
                });


        }
    }

}

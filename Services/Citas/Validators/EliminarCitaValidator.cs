namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;

    public sealed class EliminarCitaValidator : AbstractValidator<EliminarCitaContext>
    {
        private readonly PortalDbContext _db;

        public EliminarCitaValidator(PortalDbContext db)
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
                                .FirstOrDefaultAsync(cita => cita.Id == idCita && cita.ProveedorId == ctx.ProveedorId);
                            ctx.State.cita = cita;
                            return cita != null;
                        })
                        .WithMessage(x => $"La cita con id '{x.IdCita}' no existe.")
                        .WithErrorCode("Cita:NotFound")
                        .DependentRules(() =>
                        {
                            // Si existe la cita valida su estado
                            RuleFor(x => x)
                                .Must(x => x.State.cita!.Estado == "CREADA")
                                .WithMessage(x =>
                                    $"La cita con ID '{x.IdCita}' no puede ser eliminada, su estado actual es '{x.State.cita!.Estado}'.")
                                .WithErrorCode("Cita:InvalidStatusForDelete");
                        });
                });


        }
    }

}

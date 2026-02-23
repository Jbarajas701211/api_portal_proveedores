namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;
    using System.Linq;
    using System.Text.RegularExpressions;

    public sealed class IncidenciaSolicitaUrlValidator : AbstractValidator<IncidenciaSolicitaUrlValidatorContext>
    {
        private readonly PortalDbContext _db;
        public IncidenciaSolicitaUrlValidator(PortalDbContext db)
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
                                .Include(c => c.Entrega)
                                .FirstOrDefaultAsync(cita => cita.Id == idCita);
                            ctx.State.cita = cita;
                            return cita != null;
                        })
                        .WithMessage(x => $"La cita con id '{x.IdCita}' no existe.")
                        .DependentRules(() =>
                        {
                            RuleFor(x => x)
                                .Must(x => x.State.cita.Estado == EstadoCita.ENTREGADA.ToString())
                                .WithMessage(x =>
                                    $"La cita con ID '{x.IdCita}' no se encuentra en estado ENTREGADA, por tanto no se pueden registrar incidencias.")
                                .DependentRules(() => {


                                    RuleFor(x => x.IdCita)
                                        .MustAsync(async (ctx, idCita, ct) =>
                                        {
                                            var item = await _db.CitasIncidencias
                                                .FirstOrDefaultAsync(item => item.Id == ctx.Dto.IncidenciaId && item.CitaId == ctx.Dto.CitaId);
                                            ctx.State.citaIncidencia = item;
                                            return item != null;
                                        })
                                        .WithMessage(x => $"El id de incidencia: '{x.Dto.IncidenciaId}' no existe.");

                                });

                        });
                });
        }
    }

}

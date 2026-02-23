namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;
    using System.Linq;
    using System.Text.RegularExpressions;

    public sealed class IncidenciaValidator : AbstractValidator<IncidenciaValidatorContext>
    {
        private readonly PortalDbContext _db;
        public IncidenciaValidator(PortalDbContext db)
        {
            _db = db;

            RuleFor(x => x.IdCita)
                .GreaterThan(0)
                .WithMessage("El id de la cita debe ser mayor a cero.")
                .DependentRules(() =>
                {
                    // validar que la cita exista
                    RuleFor(x => x.IdCita)
                        .MustAsync(async (ctx, idCita, ct) =>
                        {
                            var cita = await _db.Citas
                                .Include(c => c.Entrega)
                                .FirstOrDefaultAsync(cita => cita.Id == idCita, ct);

                            ctx.State.cita = cita;
                            return cita != null;
                        })
                        .WithMessage(x => $"La cita con id '{x.IdCita}' no existe.")
                        .DependentRules(() =>
                        {
                            RuleFor(x => x)
                                .Must(x => x.State.cita.Estado == EstadoCita.ENTREGADA.ToString())
                                .WithMessage(x =>
                                    $"La cita con ID '{x.IdCita}' no se encuentra en estado ENTREGADA, por tanto no se puede realizar la entrega.")
                                .DependentRules(() =>
                                {

                                    // Debe venir al menos una clave
                                    RuleFor(x => x.Dto.ClavesInc)
                                        .Cascade(CascadeMode.Stop)
                                        .NotNull()
                                            .WithMessage("Debes especificar al menos una clave de incidencia.")
                                        .Must(claves => claves.Any())
                                            .WithMessage("Debes especificar al menos una clave de incidencia.")
                                        .DependentRules(() =>
                                        {
                                            RuleFor(x => x.Dto.ClavesInc)
                                                .Must(claves => claves.Distinct().Count() == claves.Count)
                                                .WithMessage("Las claves de incidencia no deben contener duplicados.");


                                            RuleFor(x => x.Dto.ClavesInc)
                                                .MustAsync(async (ctx, claves, ct) =>
                                                {
                                                    var clavesDistinct = claves.Distinct().ToList();

                                                    var count = await _db.CatalogoIncidencias
                                                        .CountAsync(item =>
                                                            clavesDistinct.Contains(item.Clave),
                                                            ct);

                                                    return count == clavesDistinct.Count;
                                                })
                                                .WithMessage("Una o más claves de incidencia no existen en el catálogo.");
                                        });


                                    RuleFor(x => x.Dto.Observacion)
                                        .Custom((raw, ctx) =>
                                        {
                                            if (raw is null) return;

                                            var v = Regex.Replace(raw, @"\s+", " ").Trim();

                                            if (Regex.IsMatch(v, @"<\s*\/?\s*\w+|<\s*script", RegexOptions.IgnoreCase))
                                                ctx.AddFailure("El parametro 'observación' no debe contener etiquetas HTML o script.");

                                            else if (v.Any(char.IsControl))
                                                ctx.AddFailure("El parametro 'observación' contiene caracteres inválidos.");
                                        });
                                });
                        });
                });
        }
    }
}

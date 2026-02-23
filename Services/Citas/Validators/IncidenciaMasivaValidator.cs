namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Google.Api;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    public sealed class IncidenciaMasivaValidator : AbstractValidator<IncidenciaMasivaValidatorContext>
    {
        private readonly PortalDbContext _db;
        public IncidenciaMasivaValidator(PortalDbContext db)
        {
            _db = db;

            RuleFor(x => x.Dto.CitasId)
                .NotNull().WithMessage("Debes enviar el arreglo de ids de cita.")
                .NotEmpty().WithMessage("Debes enviar al menos un id de cita.").DependentRules(() => {

                    RuleForEach(x => x.Dto.CitasId)
                        .GreaterThan(0)
                        .WithMessage("El id de la cita en la posición {CollectionIndex} debe ser mayor a cero (valor: {PropertyValue}).").DependentRules(() => {

                            // validar que las citas existan
                            RuleFor(x => x.IdCita)
                                .MustAsync(async (ctx, idCita, context, ct) =>
                                {

                                    var input = ctx.Dto.CitasId.Distinct().ToArray();
                                    var existentes = await _db.Citas
                                        .AsNoTracking()
                                        .Where(c => input.Contains(c.Id))
                                        .Select(c => c.Id)
                                        .ToListAsync(ct);

                                    var faltantes = input.Except(existentes).ToArray();

                                    var noExisten = string.Join(",", faltantes);

                                    context.MessageFormatter
                                           .AppendArgument("NoExisten", noExisten);

                                    return faltantes.Count() >= 0;
                                })
                                .WithMessage(x => "La ids de cita: [{NoExisten}], no existen.")
                                .DependentRules(() =>
                                {
                                    RuleFor(x => x)
                                        .MustAsync(async (ctx, idCita, context, ct) => {

                                            var input = ctx.Dto.CitasId.Distinct().ToArray();
                                            var entregadas = await _db.Citas
                                                .AsNoTracking()
                                                .Where(c => ctx.Dto.CitasId.Contains(c.Id) && c.Estado == "ENTREGADA")
                                                .Select(c => c.Id)
                                                .ToListAsync();

                                            var faltantes = input.Except(entregadas).ToArray();

                                            var noExisten = string.Join(", ", faltantes);

                                            context.MessageFormatter
                                                   .AppendArgument("NoExisten", noExisten);

                                            return !(noExisten.Length > 0);
                                        })
                                        .WithMessage(x =>
                                            "La citas con ID: [{NoExisten}] NO se encuentra en estado ENTREGADA, por tanto no se puede realizar la falla masiva.")
                                        .DependentRules(() => {


                                            RuleFor(x => x.Dto.ClaveInc)
                                                .Cascade(CascadeMode.Stop)
                                                .NotNull().WithMessage("Debe enviar al menos una clave de incidencia.")
                                                .Must(c => c.Any()).WithMessage("Debe enviar al menos una clave de incidencia.")
                                                .MustAsync(async (ctx, claves, validationContext, ct) =>
                                                {
                                                    if (claves is null || !claves.Any())
                                                        return false;

                                                    var distintos = claves.Distinct().ToArray();

                                                    var existentes = await _db.CatalogoIncidencias
                                                        .Where(ci => distintos.Contains(ci.Clave))
                                                        .Select(ci => ci.Clave)
                                                        .ToListAsync(ct);

                                                    var faltantes = distintos
                                                        .Except(existentes)
                                                        .OrderBy(v => v)
                                                        .ToArray();

                                                    validationContext.MessageFormatter
                                                        .AppendArgument("IdCita", ctx.IdCita)
                                                        .AppendArgument("Faltantes", string.Join(", ", faltantes));

                                                    return faltantes.Length == 0;
                                                })
                                                .WithErrorCode("CLAVES_INC_INEXISTENTES")
                                                .WithMessage("Las siguientes claves de incidencia no existen: [{Faltantes}]");


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
                });
        }
    }

}

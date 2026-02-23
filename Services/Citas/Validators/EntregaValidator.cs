namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;
    using System.Linq;
    using System.Text.RegularExpressions;
    using ApiProveedores.Services.Helper;
    using System;

    public sealed class EntregaValidator : AbstractValidator<EntregaValidatorContext>
    {
        private readonly PortalDbContext _db;
        private readonly HelperCita _helperCita;
        public EntregaValidator(PortalDbContext db, HelperCita helperCita)
        {
            _db = db;
            _helperCita = helperCita;

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
                                .Include(c => c.Detalles)
                                .FirstOrDefaultAsync(cita => cita.Id == idCita);
                            ctx.State.cita = cita;
                            return cita != null;
                        })
                        .WithMessage(x => $"La cita con id '{x.IdCita}' no existe.")
                        .DependentRules(() =>
                        {

                            // Si la cita no esta AGENDADA, no se puede realizar la ENTREGA.
                            RuleFor(x => x)
                                .Must((ctx, idCita, ct) => { 
                                    return ctx.State.cita.Entrega == null;
                                })
                                .WithMessage(x =>
                                    $"La cita con ID '{x.IdCita}' ya tiene una ENTREGA, por tanto ya no puede registrarse nuevamente.")
                                .DependentRules(() => {

                                    // Si la cita no esta AGENDADA, no se puede realizar la ENTREGA.
                                    RuleFor(x => x)
                                        .Must(x => x.State.cita.Estado == EstadoCita.AGENDADA.ToString())
                                        .WithMessage(x =>
                                            $"La cita con ID '{x.IdCita}' no se encuentra en estado AGENDADA, por tanto no se puede realizar la entrega.")
                                        .DependentRules(() => {



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

                                            RuleFor(x => x.Dto.CantidadEntregada)
                                                .MustAsync(async (ctx, cantidadEntregada, ct) =>
                                                {
                                                    var totalCitado = await _helperCita.CantidadCitaAsync(ctx.State.cita, ct);

                                                    return cantidadEntregada <= totalCitado;
                                                })
                                                .WithMessage(x =>
                                                    $"La cantidad entregada ({x.Dto.CantidadEntregada}) no puede ser mayor a la cantidad citada para la cita.");

                                            // valida el campo anden
                                            RuleFor(x => x.Dto.Anden)
                                                .Custom((raw, ctx) =>
                                                {
                                                    if (string.IsNullOrWhiteSpace(raw)) return;

                                                    var v = raw.Trim().ToUpperInvariant();

                                                    if (v.Length > 10)
                                                        ctx.AddFailure("anden", "El anden no debe exceder 10 caracteres.");

                                                    else if (!Regex.IsMatch(v, @"^[A-Z0-9]+(?:-[A-Z0-9]+)?$"))
                                                        ctx.AddFailure("anden", "El anden tiene el formato inválido. Usa letras/números y guion medio.");

                                                    else if (v.Any(char.IsControl))
                                                        ctx.AddFailure("anden", "El anden contiene caracteres inválidos.");
                                                });

                                            // valida el campo acuse
                                            RuleFor(x => x.Dto.Acuse)
                                                .Custom((raw, ctx) =>
                                                {
                                                    if (string.IsNullOrWhiteSpace(raw)) return;

                                                    var v = raw.Trim().ToUpperInvariant();

                                                    if (v.Length > 50)
                                                        ctx.AddFailure("acuse", "El acuse no debe exceder 50 caracteres.");

                                                    else if (!Regex.IsMatch(v, @"^[A-Z ]+$"))
                                                        ctx.AddFailure("acuse", "El acuse solo debe contener letras y espacios, sin números ni caracteres especiales.");

                                                    else if (v.Any(char.IsControl))
                                                        ctx.AddFailure("acuse", "El acuse contiene caracteres inválidos.");

                                                    ctx.InstanceToValidate.Dto.Acuse = v;
                                                });

                                            RuleFor(x => x.Dto.Notas)
                                                .Custom((raw, ctx) =>
                                                {
                                                    if (raw is null) return;

                                                    var v = Regex.Replace(raw, @"\s+", " ").Trim();

                                                    if (Regex.IsMatch(v, @"<\s*\/?\s*\w+|<\s*script", RegexOptions.IgnoreCase))
                                                        ctx.AddFailure("El parametro 'nota' no debe contener etiquetas HTML o script.");

                                                    else if (v.Any(char.IsControl))
                                                        ctx.AddFailure("El parametro 'nota' contiene caracteres inválidos.");
                                                });


                                        });

                                });

                        });
                });
        }
    }

}

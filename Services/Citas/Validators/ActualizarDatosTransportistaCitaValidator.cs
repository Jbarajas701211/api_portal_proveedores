namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Google.Api;
    using Microsoft.EntityFrameworkCore;
    using System;

    public sealed class ActualizarDatosTransportistaCitaValidator : AbstractValidator<ActualizarDatosTransportisCitaContext>
    {
        private readonly PortalDbContext _db;
        private readonly ParametroSistemaService _parametroSistemaService;
        public ActualizarDatosTransportistaCitaValidator(PortalDbContext db, ParametroSistemaService parametroSistemaService)
        {
            _db = db;
            _parametroSistemaService = parametroSistemaService;

            // validar que el id de la cita sea mayor que cero
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
                                .Include(c => c.Detalles)
                                .FirstOrDefaultAsync(cita => cita.Id == idCita && cita.ProveedorId == ctx.ProveedorId);
                            ctx.State.cita = cita;
                            return cita != null;
                        })
                        .WithMessage(x => $"La cita con id '{x.IdCita}' no existe.")
                        .WithErrorCode("Cita:NotFound");
                });

            // valida que los datos del transportista se puedan modificar hasta
            // cierto numero de horas antes de la cita, y solo si esta AGENDADA.
            RuleFor(x => x.Dto)
                .MustAsync(async (ctx, model, dto, ct) =>
                {
                    if (string.Equals(ctx.State.cita.Estado, EstadoCita.ELIMINADA.ToString(), StringComparison.OrdinalIgnoreCase)) return false;
                    return true;
                })
                .WithMessage("La cita ya ha sido eliminada, no puede ser alterada.");

            // valida que los datos del transportista se puedan modificar hasta
            // cierto numero de horas antes de la cita, y solo si esta AGENDADA.
            RuleFor(x => x.Dto)
                .MustAsync(async (model, value, context, ct) =>
                {
                    if (string.Equals(model.State.cita.Estado, EstadoCita.AGENDADA.ToString(), StringComparison.OrdinalIgnoreCase)) {
                        // Obtener el parámetro de horas permitido
                        var hrsAntesParam = await parametroSistemaService.ObtenerParametroAsync("CITA_MODIF_TRANSPORTISTA_HRS_ANTES");
                        var hrsPermitidas = int.Parse(hrsAntesParam.Valor);

                        // se le restan las horas permitidas antes de la fecha de la cita. 
                        var fechaCitaUtc = DateTime.SpecifyKind(model.State.cita.FechaCita, DateTimeKind.Utc);
                        var limiteUtc = fechaCitaUtc.AddHours(-hrsPermitidas);

                        var ahoraUtc = DateTime.UtcNow;

                        context.MessageFormatter
                               .AppendArgument("LimiteUtc", limiteUtc)
                               .AppendArgument("HorasAntes", hrsPermitidas);

                        // se valida la hora
                        return ahoraUtc <= limiteUtc;
                    }
                    return true;
                })
                .WithMessage("Solo se puede modificar el transportista hasta {HorasAntes} horas antes de la cita.");

            RuleFor(x => x.Dto.NombreChofer)
                .NotEmpty().WithMessage("El nombre del chofer es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre del chofer no debe exceder los 100 caracteres.")
                .Matches(@"^[a-zA-Z0-9ÁÉÍÓÚáéíóúÑñüÜ\s\-\.,]*$")
                    .WithMessage("El nombre del chofer contiene caracteres no permitidos.");

            RuleFor(x => x.Dto.NombreAyudante)
                .MaximumLength(100).WithMessage("El nombre del ayudante no debe exceder los 100 caracteres.")
                .Matches(@"^[a-zA-Z0-9ÁÉÍÓÚáéíóúÑñüÜ\s\-\.,]*$")
                    .When(x => !string.IsNullOrWhiteSpace(x.Dto.NombreAyudante))
                    .WithMessage("El nombre del ayudante contiene caracteres no permitidos.");

            RuleFor(x => x.Dto.TipoUnidad)
                .NotEmpty().WithMessage("El tipo de unidad es obligatorio.")
                .MaximumLength(50).WithMessage("El tipo de unidad no debe exceder los 50 caracteres.")
                .Matches(@"^[a-zA-Z0-9ÁÉÍÓÚáéíóúÑñüÜ\s\-\.,]*$")
                    .WithMessage("El tipo de unidad contiene caracteres no permitidos.");

            RuleFor(x => x.Dto.Placas)
                .NotEmpty().WithMessage("Las placas son obligatorias.")
                .MaximumLength(15).WithMessage("Las placas no deben exceder los 15 caracteres.")
                .Matches("^[A-Z0-9-]+$")
                    .WithMessage("Las placas solo pueden contener letras, números y guiones.");

            RuleFor(x => x.Dto.LineaTransportista)
                .NotEmpty().WithMessage("La línea transportista es obligatoria.")
                .MaximumLength(100).WithMessage("La línea transportista no debe exceder los 100 caracteres.")
                .Matches(@"^[a-zA-Z0-9ÁÉÍÓÚáéíóúÑñüÜ\s\-\.,]*$")
                    .WithMessage("La línea transportista contiene caracteres no permitidos.");

            RuleFor(x => x.Dto.Observaciones)
                .MaximumLength(250).WithMessage("Las observaciones no deben exceder los 250 caracteres.")
                .Matches(@"^[a-zA-Z0-9ÁÉÍÓÚáéíóúÑñüÜ\s\-\.,]*$")
                    .When(x => !string.IsNullOrWhiteSpace(x.Dto.Observaciones))
                    .WithMessage("Las observaciones contienen caracteres no permitidos.");


        }
    }

}

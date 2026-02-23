namespace ApiProveedores.Services.Citas.Validators
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using ApiProveedores.Services.Exceptions;
    using FluentValidation;

    public sealed class RegistroCitaValidator : AbstractValidator<RegistroCitaContext>
    {
        private readonly CentroDistribucionService _cdService;
        private readonly ParametroSistemaService _paramService;
        private readonly DiaNoLaborableService _noLabService;

        private TimeSpan? _ventanaInicio;
        private TimeSpan? _ventanaFinExclusivo;

        public RegistroCitaValidator(
            CentroDistribucionService cdService,
            ParametroSistemaService paramService,
            DiaNoLaborableService noLabService)
        {
            _cdService = cdService;
            _paramService = paramService;
            _noLabService = noLabService;

            // CD obligatorio y valido
            RuleFor(x => x.Dto.Cd)
                .NotEmpty()
                    .WithMessage("El CD no puede estar vacío.")
                    .WithErrorCode("CD:Required")
                .MustAsync(async (ctx, cd, ct) => await _cdService.ExisteAsync(cd!, ct))
                    .WithMessage(x => $"El centro de distribución '{x.Dto.Cd}' no existe.")
                    .WithErrorCode("CD:NotFound");

            // Proveedor valido
            RuleFor(x => x.ProveedorId)
                .GreaterThan(0)
                    .WithMessage("ID de proveedor inválido.")
                    .WithErrorCode("Proveedor:Invalid");

            // Fecha obligatoria
            RuleFor(x => x.Dto.FechaCita)
                .NotEqual(default(DateTime))
                    .WithMessage("La fecha de cita es requerida.")
                    .WithErrorCode("Fecha:Required");

            // La fecha de cita no debe de estar en el pasado
            RuleFor(x => x.Dto.FechaCita)
                .Must((ctx, fecha) =>
                {
                    var fechaUtc = DateTime.SpecifyKind(fecha, DateTimeKind.Utc).Date;
                    var hoyUtc = ctx.NowUtc.Date;
                    return fechaUtc >= hoyUtc;
                })
                .WithMessage("La fecha de la cita debe ser igual o posterior a la fecha actual.")
                .WithErrorCode("Fecha:PastNotAllowed");

            // Valida el los dias maximos para hacer el registro de la cita
            RuleFor(x => x.Dto.FechaCita)
                .MustAsync(async (model, fechaCita, context, ct) =>
                {
                    var maxDias = await _paramService.ObtenerParametroAsync("CITA_MAXIMO_DIAS_PARA_REGISTRO");
                    var max = int.Parse(maxDias.Valor);

                    var fechaCitaUtc = DateTime.SpecifyKind(fechaCita, DateTimeKind.Utc).Date;
                    var limiteUtc = model.NowUtc.Date.AddDays(max);

                    context.MessageFormatter
                           .AppendArgument("LimiteUtc", limiteUtc)
                           .AppendArgument("MaxDias", max);

                    return fechaCitaUtc <= limiteUtc;
                })
                .WithMessage("La fecha de la cita no puede ser posterior a {LimiteUtc:yyyy-MM-dd} (máximo {MaxDias} días desde hoy).")
                .WithErrorCode("Fecha:BeyondMaxDays");

            // Valida la ventana horaria CITA_REGISTRO_HORA_INI y CITA_REGISTRO_HORA_FIN
            RuleFor(x => x.Dto.HoraCita)
                .MustAsync(async (model, hora, context, ct) =>
                {
                    await ValidaVentanaDeTiempoAsync(ct);

                    // Inyecta datos para el mensaje:
                    context.MessageFormatter
                           .AppendArgument("Inicio", _ventanaInicio!.Value)
                           .AppendArgument("Fin", _ventanaFinExclusivo!.Value)
                           .AppendArgument("Hora", hora);

                    return hora >= _ventanaInicio!.Value && hora < _ventanaFinExclusivo!.Value;
                })
                .WithMessage("La hora de la cita debe estar entre {Inicio:hh\\:mm} y {Fin:hh\\:mm} ({Hora:hh\\:mm} no permitido).")
                .WithErrorCode("Hora:OutOfWindow");

            RuleFor(x => x.Dto.Lote)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("El lote es obligatorio.")
                .Matches(@"^[1-9]$").WithMessage("El lote debe ser un entero entre 1 y 9.");

            RuleFor(x => x.Dto.FechaCita)
                .MustAsync(async (ctx, fecha, ct) =>
                {
                    var f = DateTime.SpecifyKind(fecha, DateTimeKind.Utc).Date;
                    var esNoLab = await _noLabService.EsNoLaborableAsync(f);
                    return !esNoLab;
                })
                .WithMessage("La fecha de la cita se está registrando en una fecha NO laborable.")
                .WithErrorCode("Fecha:NoLaborable");

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

        public async Task ValidaVentanaDeTiempoAsync(CancellationToken ct)
        {
            if (_ventanaInicio.HasValue && _ventanaFinExclusivo.HasValue) return;

            var iniStr = await _paramService.ObtenerParametroAsync("CITA_REGISTRO_HORA_INI");
            var finStr = await _paramService.ObtenerParametroAsync("CITA_REGISTRO_HORA_FIN");

            var inicio = ParseHHmmOrThrow(iniStr.Valor, "CITA_REGISTRO_HORA_INI");
            var fin = ParseHHmmOrThrow(finStr.Valor, "CITA_REGISTRO_HORA_FIN");

            if (inicio >= fin)
                throw new CitaException("Configuración inválida: CITA_REGISTRO_HORA_INI debe ser menor que CITA_REGISTRO_HORA_FIN.");

            _ventanaInicio = inicio;
            _ventanaFinExclusivo = fin;
        }

        private static TimeSpan ParseHHmmOrThrow(string? s, string nombreParametro)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new CitaException($"Falta el parámetro {nombreParametro} (formato esperado HH:mm).");

            if (!TimeSpan.TryParseExact(s.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var ts))
                throw new CitaException($"Formato inválido en {nombreParametro}: '{s}'. Usa HH:mm (ej. 08:00).");

            return ts;
        }
    }

}

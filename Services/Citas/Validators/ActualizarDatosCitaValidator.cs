namespace ApiProveedores.Services.Citas.Validators
{
    using ApiProveedores.Services.Exceptions;
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using System.Threading;

    public sealed class ActualizarDatosCitaValidator : AbstractValidator<ActualizarDatosCitaContext>
    {
        private readonly PortalDbContext _db;
        private readonly ParametroSistemaService _paramService;
        private readonly CentroDistribucionService _cdService;
        private readonly DiaNoLaborableService _noLabService;

        private TimeSpan? _ventanaInicio;
        private TimeSpan? _ventanaFinExclusivo;

        public ActualizarDatosCitaValidator(PortalDbContext db, 
            ParametroSistemaService parametroSistemaService,
            DiaNoLaborableService noLabService,
            CentroDistribucionService cdService)
        {
            _db = db;
            _paramService = parametroSistemaService;
            _cdService = cdService;

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
                        .WithErrorCode("Cita:NotFound").DependentRules(() => {
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
                                    var hoyUtc = DateTime.UtcNow.Date;
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
                                    var limiteUtc = DateTime.UtcNow.Date.AddDays(max);

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


                            // ---- No laborable
                            RuleFor(x => x.Dto.FechaCita)
                                .MustAsync(async (ctx, fecha, ct) =>
                                {
                                    var f = DateTime.SpecifyKind(fecha, DateTimeKind.Utc).Date;
                                    var esNoLab = await _noLabService.EsNoLaborableAsync(f);
                                    return !esNoLab;
                                })
                                .WithMessage("La fecha de la cita se está registrando en una fecha NO laborable.")
                                .WithErrorCode("Fecha:NoLaborable");

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
                                    if (string.Equals(model.State.cita.Estado, EstadoCita.AGENDADA.ToString(), StringComparison.OrdinalIgnoreCase))
                                    {
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

                        });
                });


            _noLabService = noLabService;
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

namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System;
    using System.Linq;
    using ApiProveedores.Models;
    using Microsoft.AspNetCore.Http.HttpResults;

    public sealed class GenerarFolioCitaValidator : AbstractValidator<GenerarFolioCitaContext>
    {
        private readonly PortalDbContext _db;
        public GenerarFolioCitaValidator(PortalDbContext db, ParametroSistemaService parametroSistemaService)
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
                        .DependentRules(() =>
                        {

                            RuleFor(x => x.IdCita)
                                .Must((ctx, _) =>
                                {
                                    var cita = ctx.State.cita;
                                    return cita != null &&
                                           cita.Detalles != null &&
                                           cita.Detalles.Count > 0;
                                })
                                .WithMessage("La cita no tiene ningún detalle asignado.").DependentRules(() => {




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






                                });

                            // Si la cita ya esta AGENDADA, no se puede volver a agendar.
                            RuleFor(x => x)
                                .Must(x => x.State.cita.Estado != EstadoCita.AGENDADA.ToString())
                                .WithMessage(x =>
                                    $"La cita con ID '{x.IdCita}' ya se encuentra en estado AGENDADA y no puede volver a ser agendada.").DependentRules(() => {
                                        // Si existe la cita valida su estado
                                        RuleFor(x => x)
                                            .Must(x => {
                                                return
                                                    x.State.cita.Estado == EstadoCita.CREADA.ToString() ||
                                                    x.State.cita.Estado == EstadoCita.AUTORIZADA.ToString() ||
                                                    x.State.cita.Estado == EstadoCita.DENEGADA.ToString() ||
                                                    x.State.cita.Estado == EstadoCita.REAGENDADA.ToString();
                                            })
                                            .WithMessage(x =>
                                                $"La cita con ID '{x.IdCita}' solo puede cambiar a AGENDADA si está en estado CREADA, AUTORIZADA o REAGENDADA. Estado actual: '{x.State.cita!.Estado}'.")
                                                .DependentRules(() => {

                                                    // Valida si no choca con alguna otra cita asignada
                                                    RuleFor(x => x.State.cita.Detalles)
                                                        .MustAsync(async (model, detalles, context, ct) => {

                                                            var ordenesExistenEnOtrasCitas = (detalles ?? Enumerable.Empty<CitaDetalle>())
                                                                .Select(d => d.Oc?.Trim())
                                                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                                                .ToList();


                                                            if (ordenesExistenEnOtrasCitas.Count == 0)
                                                                // si no hay ordenes asignadas en otras citas, procede con las validaciones siguientes
                                                                return true;

                                                            var ocsEnConflicto = await db.CitasDetalle
                                                                .Where(cd => ordenesExistenEnOtrasCitas.Contains(cd.Oc))
                                                                .Where(cd => cd.Cita.Estado == "AGENDADA")
                                                                .Where(cd => cd.CitaId != model.IdCita)
                                                                .Select(cd => new { cd.Oc, cd.CitaId })
                                                                .Distinct()
                                                            .ToListAsync(ct);

                                                            if (!ocsEnConflicto.Any()) return true;

                                                            var porOc = ocsEnConflicto
                                                                .GroupBy(x => x.Oc)
                                                                .ToDictionary(
                                                                    g => g.Key!,
                                                                    g => g.Select(i => i.CitaId).Distinct().OrderBy(id => id).ToList()
                                                                );

                                                            var detalleMsg = string.Join("; ", porOc.Select(kvp => $"{kvp.Key}: citas id: [{string.Join(", ", kvp.Value)}]"));
                                                            context.MessageFormatter.AppendArgument("OCs", string.Join(", ", ocsEnConflicto));
                                                            return false;
                                                        })
                                                        .WithMessage(x => $"La cita con ID '{x.IdCita}' tiene órdenes ya asignadas a otra cita AGENDADA: {{OCs}}.");

                                                    // valida las capacidades de los almacenes para agendar la cita
                                                    RuleFor(x => x)
                                                        .MustAsync(async (_, context, validationContext, ct) =>
                                                        {

                                                            var origenes = new[] { "PRE", "STK" };

                                                            var totales = await _db.CapacidadCdOrigen
                                                                .Join(_db.CatalogoOrigenCapacidad,
                                                                      cd => cd.Origen,
                                                                      o => o.Clave,
                                                                      (cd, o) => new { cd, o })
                                                                .Where(x => origenes.Contains(x.o.ClaveOrigen))
                                                                .GroupBy(x => x.o.ClaveOrigen)
                                                                .Select(g => new
                                                                {
                                                                    ClaveOrigen = g.Key,
                                                                    Total = g.Sum(x => (long?)x.cd.CapacidadMaxima) ?? 0L
                                                                })
                                                                .ToListAsync();

                                                            var capacidadesMaximas = origenes
                                                                .ToDictionary(
                                                                    k => k,
                                                                    k => totales.FirstOrDefault(t => t.ClaveOrigen == k)?.Total ?? 0L
                                                                );

                                                            var fechaCitaUtc = DateTime.SpecifyKind(context.State.cita.FechaCita.Date, DateTimeKind.Utc);

                                                            var capacidadUtilizada = await _db.CapacidadResumenCd
                                                                .Join(_db.CatalogoOrigenCapacidad,
                                                                      c => c.Origen,
                                                                      o => o.Clave,
                                                                      (c, o) => new { c, o })
                                                                .Where(x => origenes.Contains(x.o.ClaveOrigen)
                                                                         && x.c.Fecha == fechaCitaUtc.Date)
                                                                .GroupBy(x => x.o.ClaveOrigen)
                                                                .Select(g => new
                                                                {
                                                                    ClaveOrigen = g.Key,
                                                                    Total = g.Sum(x => (int?)x.c.CapacidadUtilizada) ?? 0
                                                                })
                                                                .ToListAsync();

                                                            var capacidadesUtilizadas = origenes
                                                                .ToDictionary(
                                                                    k => k,
                                                                    k => capacidadUtilizada.FirstOrDefault(t => t.ClaveOrigen == k)?.Total ?? 0
                                                                );

                                                            // se calculan las capacidades disponibles
                                                            var capacidadesDisponibles = origenes
                                                                .ToDictionary(
                                                                    k => k,
                                                                    k => (capacidadesMaximas.ContainsKey(k) ? capacidadesMaximas[k] : 0L)
                                                                       - (capacidadesUtilizadas.ContainsKey(k) ? capacidadesUtilizadas[k] : 0)
                                                                );

                                                            // obtiene los totales por origen del detalle de la cita
                                                            var totalesCita = context.State.cita.Detalles
                                                                .Where(d => origenes.Contains(d.Origen))
                                                                .GroupBy(d => d.Origen)
                                                                .Select(g => new
                                                                {
                                                                    Origen = g.Key,
                                                                    Total = g.Sum(d => d.CantidadPorCita)
                                                                })
                                                                .ToDictionary(x => x.Origen, x => x.Total);

                                                            var totalesOrigenesCita = origenes
                                                                .ToDictionary(
                                                                    k => k,
                                                                    k => totalesCita.FirstOrDefault(t => t.Key == k).Value
                                                                );


                                                            bool esValida = totalesOrigenesCita.All(kv =>
                                                            {
                                                                var origen = kv.Key;
                                                                var solicitado = kv.Value;
                                                                var disponible = capacidadesDisponibles.ContainsKey(origen)
                                                                    ? capacidadesDisponibles[origen]
                                                                    : 0;

                                                                return solicitado <= disponible;
                                                            });



                                                            var errores = totalesOrigenesCita
                                                                .Where(kv =>
                                                                {
                                                                    var origen = kv.Key;
                                                                    var solicitado = kv.Value;
                                                                    var disponible = capacidadesDisponibles.ContainsKey(origen)
                                                                        ? capacidadesDisponibles[origen]
                                                                        : 0;
                                                                    return solicitado > disponible;
                                                                })
                                                                .ToList();

                                                            var capacidadValidada = errores.Count == 0;

                                                            if (!capacidadValidada)
                                                            {
                                                                var detalle = string.Join("; ", errores);

                                                                validationContext.MessageFormatter
                                                                    .AppendArgument("IdCita", context.IdCita)
                                                                    .AppendArgument("FechaCita", context.State.cita.FechaCita.ToString("yyyy-MM-dd"))
                                                                    .AppendArgument("DetalleCapacidad", detalle);
                                                            }

                                                            return capacidadValidada;

                                                        })
                                                        .WithMessage("Capacidad insuficiente para la cita '{IdCita}'. Fecha de cita: {FechaCita}. Detalle: {DetalleCapacidad}. Puede solicitar autorización al equipo de logística.");





                                                    RuleFor(x => x)
                                                        .MustAsync(async (_, context, validationContext, ct) =>
                                                        {
                                                            var raw = await parametroSistemaService.ObtenerParametroAsync("CITA_MINIMO_HORAS_PARA_CITAR");
                                                            int.TryParse(raw.Valor, out var minHoras);

                                                            var raw2 = await parametroSistemaService.ObtenerParametroAsync("CITA_MAXIMO_HORAS_PARA_CITAR");
                                                            int.TryParse(raw2.Valor, out var maxHoras);

                                                            var raw3 = await parametroSistemaService.ObtenerParametroAsync("CITA_MINIMO_HORAS_PARA_SOLICITAR_AUTORIZACION");
                                                            int.TryParse(raw3.Valor, out var minHorasParaSolicitarAutorizacion);

                                                            var raw4 = await parametroSistemaService.ObtenerParametroAsync("CITA_MAXIMO_HORAS_PARA_SOLICITAR_AUTORIZACION");
                                                            int.TryParse(raw4.Valor, out var maxHorasParaSolicitarAutorizacion);

                                                            // zona horaria MX
                                                            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
                                                            var fechaHoraCitaLocal = context.State.cita.FechaCita.Date
                                                                                                .Add(context.State.cita.HoraCita);
                                                            var ahoraMx = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

                                                            // horas restantes a la cita
                                                            var horasRestantes = (fechaHoraCitaLocal - ahoraMx).TotalHours;
                                                            var ventanaMin = ahoraMx.AddHours(minHoras);
                                                            var ventanaMax = ahoraMx.AddHours(maxHoras);

                                                            // ventana 1: para citar
                                                            var dentroVentanaCitar = horasRestantes >= minHoras && horasRestantes <= maxHoras;

                                                            // ventana 2: para solicitar autorización
                                                            var dentroVentanaAutorizacion =
                                                                horasRestantes >= minHorasParaSolicitarAutorizacion &&
                                                                horasRestantes <= maxHorasParaSolicitarAutorizacion;

                                                            // mensaje extra solo si aplica el tema de autorización
                                                            var msgAutorizacion = dentroVentanaAutorizacion && !dentroVentanaCitar
                                                                ? "|O Solicite Autorización para validar si procede la cita."
                                                                : "";

                                                            validationContext.MessageFormatter
                                                                .AppendArgument("IdCita", context.IdCita)
                                                                .AppendArgument("FechaCita", fechaHoraCitaLocal.ToString("dd/MM/yyyy HH:mm"))
                                                                .AppendArgument("HorasRestantes", Math.Round(horasRestantes, 1))
                                                                .AppendArgument("MinHoras", minHoras)
                                                                .AppendArgument("MaxHoras", maxHoras)
                                                                .AppendArgument("VentanaMin", ventanaMin.ToString("dd/MM/yyyy HH:mm"))
                                                                .AppendArgument("VentanaMax", ventanaMax.ToString("dd/MM/yyyy HH:mm"))
                                                                .AppendArgument("Autorizacion", msgAutorizacion);

                                                            // procede si está en la ventana de CITAR o en la de AUTORIZAR
                                                            var procede = dentroVentanaCitar || dentroVentanaAutorizacion;
                                                            if (procede) {
                                                                context.State.suceptibleParaSolicitarValidacion = dentroVentanaAutorizacion;
                                                            }
                                                            
                                                            return procede;
                                                        })
                                                        .WithErrorCode("VENTANA_48_120")
                                                        .WithMessage("La cita '{IdCita}' solo puede registrarse entre {MinHoras} y {MaxHoras} horas de anticipación. " +
                                                                     "Fecha/hora de cita: {FechaCita}.|" +
                                                                     "Selecciona una fecha cuya anticipación caiga entre {VentanaMin} y {VentanaMax}." +
                                                                     "{Autorizacion}");



                                                });



                                    });



                        });
                });


        }
    }

}

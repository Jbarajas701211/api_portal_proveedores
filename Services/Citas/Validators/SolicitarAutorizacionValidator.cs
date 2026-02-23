namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System;
    using System.Linq;
    using ApiProveedores.Models;
    using Microsoft.AspNetCore.Http.HttpResults;

    public sealed class SolicitarAutorizacionValidator : AbstractValidator<SolicitarAutorizacionContext>
    {
        private readonly PortalDbContext _db;
        public SolicitarAutorizacionValidator(PortalDbContext db, ParametroSistemaService parametroSistemaService)
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
                                .Include(c => c.Proveedor)
                                .FirstOrDefaultAsync(cita => cita.Id == idCita && cita.ProveedorId == ctx.ProveedorId);
                            ctx.State.cita = cita;
                            return cita != null;
                        })
                        .WithMessage(x => $"La cita con id '{x.IdCita}' no existe.")
                        .DependentRules(() =>
                        {

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
                                                    x.State.cita.Estado == EstadoCita.REAGENDADA.ToString() ||
                                                    x.State.cita.Estado == EstadoCita.DENEGADA.ToString();
                                            })
                                            .WithMessage(x =>
                                                $"La cita con ID '{x.IdCita}' solo puede cambiar a AGENDADA si está en estado CREADA o REAGENDADA. Estado actual: '{x.State.cita!.Estado}'.");
                                    });
                        });
                });


        }
    }

}

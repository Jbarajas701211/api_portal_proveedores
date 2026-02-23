namespace ApiProveedores.Services.Citas.Validators
{
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System;
    using System.Linq;
    using ApiProveedores.Models;
    using Microsoft.AspNetCore.Http.HttpResults;

    public sealed class AutorizarDenegarValidator : AbstractValidator<CitaContext>
    {
        private readonly PortalDbContext _db;
        public AutorizarDenegarValidator(PortalDbContext db, ParametroSistemaService parametroSistemaService)
        {
            _db = db;

            // validar que el id de la cita sea mayor que cero
            RuleFor(x => x.IdCita)
                .GreaterThan(0)
                .WithMessage("El id de la cita debe ser mayor a cero.")
                .DependentRules(() => {

                    // validar que la cita exista y le pertenezca al proveedor a autorizar. 
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
                            // Si existe la cita valida su estado
                            RuleFor(x => x)
                                .Must(x => {
                                    return
                                        x.State.cita.Estado == EstadoCita.SOLICITA_AUTORIZACION.ToString();
                                })
                                .WithMessage(x =>
                                    $"La cita con ID '{x.IdCita}' solo será 'AUTORIZADA' o 'DENEGADA' si el estado actual es 'SOLICITA_AUTORIZACION'. Estado actual: '{x.State.cita!.Estado}'.");
                        });
                });


        }
    }

}

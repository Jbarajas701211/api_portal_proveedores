using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Helper;
using ApiProveedores.Models;
using ApiProveedores.Services.Citas.Validators;
using ApiProveedores.Services.Exceptions;
using FluentValidation;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProveedores.Services.Citas
{
    public class CitaService
    {
        private readonly PortalDbContext _context;
        private readonly ActualizaResumenService _actualizaResumenService;
        private readonly RegistroCitaValidator _registroCitaValidator;
        private readonly EliminarCitaValidator _eliminarCitaValidator;
        private readonly GenerarFolioCitaValidator _generarFolioCitaValidator;
        private readonly SolicitarAutorizacionValidator _solicitarAutorizacionValidator;
        private readonly AutorizarDenegarValidator _autorizarDenegarValidator;
        private readonly CantidadesTeoricasService _cantidadesTeoricasService;
        private readonly CapacidadService _capacidadService;
        private readonly NotificacionesService _notificacionesService;
        private readonly UsuariosService _usuariosService;
        private readonly ActualizarDatosCitaValidator _validatorActualizadatos;
        private readonly CancelacionValidator _cancelacionValidator;
        private readonly OrigenCapacidadService _origenCapacidadService;
        private readonly HelperOrdenService _helperOrdenService;


        public CitaService(PortalDbContext context,
            ActualizaResumenService actualizaResumenService,
            RegistroCitaValidator registroCitaValidator,
            EliminarCitaValidator eliminarCitaValidator, 
            GenerarFolioCitaValidator generarFolioCitaValidator,
            CantidadesTeoricasService cantidadesTeoricasService,
            SolicitarAutorizacionValidator solicitarAutorizacionValidator,
            NotificacionesService notificacionesService,
            AutorizarDenegarValidator autorizarDenegarValidator,
            UsuariosService usuariosService,
            ActualizarDatosCitaValidator validatorActualizadatos,
            CapacidadService capacidadService,
            CancelacionValidator cancelacionValidator,
            OrigenCapacidadService origenCapacidadService,
            HelperOrdenService helperOrdenService)
        {
            _context = context;
            _actualizaResumenService = actualizaResumenService;
            _registroCitaValidator = registroCitaValidator;
            _eliminarCitaValidator = eliminarCitaValidator;
            _generarFolioCitaValidator = generarFolioCitaValidator;
            _cantidadesTeoricasService = cantidadesTeoricasService;
            _solicitarAutorizacionValidator = solicitarAutorizacionValidator;
            _notificacionesService = notificacionesService;
            _autorizarDenegarValidator = autorizarDenegarValidator;
            _usuariosService = usuariosService;
            _validatorActualizadatos = validatorActualizadatos;
            _cancelacionValidator = cancelacionValidator;
            _capacidadService = capacidadService;
            _origenCapacidadService = origenCapacidadService;
            _helperOrdenService = helperOrdenService;
        }

        private async Task MarcarParaSolicitudDeAutorizacionAsync(long idCita, bool marcar, CancellationToken ct = default) {
            // Se marca la cita para que el proveedor pueda solicitar autorizacion si asi lo requiere.
            await _context.Citas
                .Where(c => c.Id == idCita)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.MarcadaParaSolicitarAutorizacion, _ => marcar));

            await _context.SaveChangesAsync(ct);
        }


        public async Task AutorizarDenegarSolicitudAsync(
            long idCita,
            long idProveedor,
            bool autorizar = false,
            CancellationToken ct = default)
        {
            var context = new CitaContext { IdCita = idCita, ProveedorId = idProveedor };
            var result = await _autorizarDenegarValidator.ValidateAsync(context, ct);

            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                if (autorizar)
                {
                    // cambio de estado a AUTORIZADA
                    await _context.Citas
                        .Where(c => c.Id == idCita && c.ProveedorId == idProveedor)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(c => c.Estado, _ => EstadoCita.AUTORIZADA.ToString()), ct);

                    // genera trazabilidad operativa para la cita que solicito autorizacion
                    await _actualizaResumenService.ActualizaResumen(context.State.cita, EstadoCita.AUTORIZADA);

                    // notificar a proveedor
                    await _notificacionesService.CrearNotificacionAsync(
                        fecha: DateTime.Now,
                        hora: DateTime.Now.TimeOfDay,
                        titulo: "Solicitud AUTORIZADA",
                        tag: "autorizada",
                        detalle: $"Se ha autorizado la cita con ID: '{context.State.cita.Id}' para que continue con su proceso.",
                        rol: "PROVEEDOR",
                        usuarioIds: new List<long> { (long)context.State.cita.RegistradoPorId }
                    );
                }
                else
                {
                    // cambio de estado a DENEGADA
                    await _context.Citas
                        .Where(c => c.Id == idCita && c.ProveedorId == idProveedor)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(c => c.Estado, _ => EstadoCita.DENEGADA.ToString()), ct);

                    // genera trazabilidad operativa para la cita que solicito autorizacion
                    await _actualizaResumenService.ActualizaResumen(context.State.cita, EstadoCita.DENEGADA);

                    // notificar a proveedor
                    await _notificacionesService.CrearNotificacionAsync(
                        fecha: DateTime.Now,
                        hora: DateTime.Now.TimeOfDay,
                        titulo: "Solicitud DENEGADA",
                        tag: "denegada",
                        detalle: $"Se ha rechazado la cita con ID: '{context.State.cita.Id}'.",
                        rol: "PROVEEDOR",
                        usuarioIds: new List<long> { (long)context.State.cita.RegistradoPorId }
                    );
                }

                // guardar y confirmar transacción
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }



        public async Task SolicitarAutorizacionAsync(long idCita, long idProveedor, CancellationToken ct = default)
        {
            var context = new SolicitarAutorizacionContext { IdCita = idCita, ProveedorId = idProveedor };
            var result = await _solicitarAutorizacionValidator.ValidateAsync(context, ct);

            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            // Cambio de estado a SOLICITA_AUTORIZACION
            await _context.Citas
            .Where(c => c.Id == idCita && c.ProveedorId == idProveedor)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Estado, _ => EstadoCita.SOLICITA_AUTORIZACION.ToString())
                .SetProperty(c => c.MarcadaParaSolicitarAutorizacion, _ => false));

            await _context.SaveChangesAsync(ct);


            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            // genera json con metadatos
            var json = JsonSerializer.Serialize(new { 
                id_cita = idCita,
                id_proveedor = idProveedor
            }, options);

            // notificar a logistica
            await _notificacionesService.CrearNotificacionAsync(
                fecha: DateTime.Now,
                hora: DateTime.Now.TimeOfDay,
                titulo: "Proveedor solicita autorización",
                tag: "solicita_autorizacion",
                detalle: $"El proveedor '{context.State.cita.Proveedor.Nombre}' ha solicitado autorización para la cita con ID: '{context.State.cita.Id}' para que continue con su proceso.",
                rol: "LOGISTICA"
            );


            // genera trazabilidad operativa para la cita que solicito autorizacion
            await _actualizaResumenService.ActualizaResumen(context.State.cita, EstadoCita.SOLICITA_AUTORIZACION);

        }

        public async Task CancelarAsync(
            long idCita,
            long idUsurio,
            long idProveedor,
            CancellationToken ct = default)
        {
            var context = new CancelacionContext { IdCita = idCita, ProveedorId = idProveedor };
            var result = await _cancelacionValidator.ValidateAsync(context, ct);

            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                // Cambio de estado a CANCELADA
                await _context.Citas
                    .Where(c => c.Id == idCita && c.ProveedorId == idProveedor)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.Estado, _ => EstadoCita.CANCELADA.ToString())
                        .SetProperty(c => c.MarcadaParaSolicitarAutorizacion, _ => false),
                        ct);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // genera json con metadatos
                var json = JsonSerializer.Serialize(new
                {
                    id_cita = idCita,
                    id_proveedor = idProveedor
                }, options);

                await _notificacionesService.CrearNotificacionAsync(
                    fecha: DateTime.Now,
                    hora: DateTime.Now.TimeOfDay,
                    titulo: "Cancelación de cita",
                    tag: "cancela-cita",
                    detalle: $"El proveedor '{context.State.cita.Proveedor.Nombre}' ha cancelado la cita con id [{idCita}]",
                    metadata: json,
                    rol: "LOGISTICA");

                // actualiza cantidades teoricas para el proceso de cancelacion.
                await _cantidadesTeoricasService.ActualizaCantidadesTeoricasAsync(context.State.cita, false, ct);

                // genera trazabilidad operativa para la cita cancelada
                await _actualizaResumenService.ActualizaResumen(context.State.cita, EstadoCita.CANCELADA);


                // bloque ordenes del detalle, para procesamiento 
                await _helperOrdenService.TraceOrdenesDeProcesamientoAsync(
                    context.State.cita, idUsurio,
                    $"Orden bloqueada para cancelar la Entrega/Entrante en SAP (por motivo de CANCELACION), para la cita: {context.State.cita.Id}",
                    TipoOperacion.BLOQUEADA,
                    ct);

                // guarda cambios y confirma la transaccion
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }


        public async Task GenerarFolioAsync(long idCita, long idProveedor, long idUsuario, bool usuarioSolicitaAutorizacion = false, CancellationToken ct = default)
        {
            /*
             * El generar el folio de la cita, implica una serie de validaciones que tienen que ser ejecutadas
             * para poder culminar ese proces, las cuales son las siguientes:
             * 
             * REGLAS DE NEGOCIO:
             * 
             *    1) Solo puede ser agendada si la cita tiene el estado CREADA o REAGENDADA.
             *    2) Que ninguna orden dentro del detalle de la cita, no exista en alguna otra CITA AGENDADA.
             *    3) Que el numero de piezas de la cita no rebase el numero de piezas de la orden 
             *       (incluyendo lo ya entregado en citas ya entregadas realmente).
             *    4) Que la cita pueda ser registrada CITA_MAXIMO_HORAS_PARA_CITAR antes de generar el folio de cita. 
             *    5) Validar que el CEDIS pueda hacer la recepcion de las piezas y/o tenga disponibilidad por origen.
             */
            var context = new GenerarFolioCitaContext { IdCita = idCita, ProveedorId = idProveedor };

            // Ejecuta secuencia de validaciones para generar el folio de la cita. 
            
            var result = await _generarFolioCitaValidator.ValidateAsync(context, ct);
            var errores = result.Errors.Select(e => $"{e.ErrorMessage}");

            if (!result.IsValid)
            {
                throw new CitaException(string.Join("|", errores));
            }


            if (context.State.suceptibleParaSolicitarValidacion && context.State.cita.Estado != "AUTORIZADA")
            {
                if (usuarioSolicitaAutorizacion)
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        await MarcarParaSolicitudDeAutorizacionAsync(context.State.cita.Id, true, ct);

                        /*
                         * Al requerir validacion la cita se deja en el estado actual 'CREADA'
                         * para primero solicitar la autorizacion por parte de usuarios de logistica
                         * y pasa a SOLICITA_AUTORIZACION y que puede ser AUTORIZADA o DENEGADA. 
                         * 
                         * Despues de ser AUTORIZADA es ahi cuando debe de pasar a SOLICITA_VALIDACION.
                         * 
                         */

                        // Cambio de estado a SOLICITA_AUTORIZACION
                        await _context.Citas
                        .Where(c => c.Id == idCita && c.ProveedorId == idProveedor)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(c => c.Estado, _ => EstadoCita.SOLICITA_AUTORIZACION.ToString()));

                        await _context.SaveChangesAsync(ct);

                        // actualiza cantidades teorias para el proceso de generacion de folio.
                        await _cantidadesTeoricasService.ActualizaCantidadesTeoricasAsync(context.State.cita);

                        // genera trazabilidad operativa para la cita a solicita validacion.
                        await _actualizaResumenService.ActualizaResumen(context.State.cita, EstadoCita.SOLICITA_AUTORIZACION);

                        // confirma transaccion.
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }

                }
                else {
                    throw new CitaException("Solicite autorización al equipo de logistica o cambie la fecha de su cita.");
                }
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Cambio de estado a AGENDADA
                    await _context.Citas
                    .Where(c => c.Id == idCita && c.ProveedorId == idProveedor)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.Estado, _ => EstadoCita.AGENDADA.ToString()));

                    await _context.SaveChangesAsync(ct);

                    // actualiza cantidades teorias para el proceso de generacion de folio.
                    await _cantidadesTeoricasService.ActualizaCantidadesTeoricasAsync(context.State.cita);

                    // genera trazabilidad operativa para la cita agendada.
                    await _actualizaResumenService.ActualizaResumen(context.State.cita, EstadoCita.AGENDADA);

                    // bloque ordenes del detalle, para procesamiento 
                    await _helperOrdenService.TraceOrdenesDeProcesamientoAsync(
                        context.State.cita, idUsuario, 
                        $"Orden bloqueada para generar la Entrega-Entrante en SAP, para la cita: {context.State.cita.Id}",
                        TipoOperacion.BLOQUEADA, 
                        ct);

                    // registra las capacidades.


                    foreach (var d in context.State.cita.Detalles)
                    {
                        await _capacidadService.RegistrarCapacidadUsoAsync(
                            context.State.cita.Cd,
                            d.ClaveAlmacen, // clave almacen o clave origen.
                            TimeHelper.UtcNow(),
                            d.CantidadPorCita,
                            "ASIGNACION",
                            (int)idUsuario);
                    }

                    // confirma transaccion.
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    throw e;
                }


            }
        }

        public async Task EliminarCitaAsync(long idCita, long idProveedor, CancellationToken ct = default)
        {

            var context = new EliminarCitaContext { IdCita = idCita, ProveedorId = idProveedor };

            // Ejecuta secuencia de validaciones para eliminar la cita. 
            var result = await _eliminarCitaValidator.ValidateAsync(context, ct);
            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            // Se eliminan relaciones con las OCs
            context.State.cita.Detalles.Clear();
            context.State.cita.Estado = EstadoCita.ELIMINADA.ToString();

            await _context.Citas
                .Where(c => c.Id == idCita && c.ProveedorId == idProveedor)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Estado, _ => EstadoCita.ELIMINADA.ToString()));
            
            await _context.SaveChangesAsync(ct);

            // genera trazabilidad operativa para la cita eliminada.
            await _actualizaResumenService.ActualizaResumen(context.State.cita, EstadoCita.ELIMINADA);
        }

        public async Task<Cita> RecuperaCitaAsync(long idCita, long idProveedor)
        {
            if (!(idCita > 0))
                throw new CitaException("El id de la cita tiene que ser mayor a cero.");

            var cita = await _context.Citas
              .FirstOrDefaultAsync(cita => cita.Id == idCita && cita.ProveedorId == idProveedor);

            return cita;
        }

        public async Task<Cita> RecuperaCitaAsync(string publicId, long idProveedor)
        {
            if (string.IsNullOrEmpty(publicId))
                throw new CitaException("El publicId de la cita no debe ser vacia.");

            Guid.TryParse(publicId, out var gid);

            var cita = await _context.Citas
              .FirstOrDefaultAsync(cita => cita.PublicId == gid && cita.ProveedorId == idProveedor);

            return cita;
        }

        public async Task<Cita> RecuperaCitaAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                throw new CitaException("El publicId de la cita no debe ser vacia.");

            Guid.TryParse(publicId, out var gid);

            var cita = await _context.Citas
              .FirstOrDefaultAsync(cita => cita.PublicId == gid);

            return cita;
        }

        public async Task<string> RegistrarCitaAsync(CrearCitaDto crearCitaDto, long idProveedor, long userId, CancellationToken ct = default)
        {
            var ctx = new RegistroCitaContext(crearCitaDto, idProveedor, DateTime.UtcNow);
            var result = await _registroCitaValidator.ValidateAsync(ctx, ct);
            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            var usuario = await _usuariosService.ObtenerUsuarioPorIdAsync(userId);
            var folioLote = CitaHelper.GenerarFolioLote(idProveedor, crearCitaDto.Lote);

            // Registra lote
            await RegistrarLoteAsync(folioLote, idProveedor);
            var fechaCitaUtc = DateTime.SpecifyKind(crearCitaDto.FechaCita, DateTimeKind.Utc);
            var nuevaCita = new Cita
            {
                CreadoEn = TimeHelper.NowMexicoUnspecified(),
                Lote = folioLote,
                NombreSolicitante = usuario.NombreCompleto,
                NombreChofer = crearCitaDto.NombreChofer,
                NombreAyudante = crearCitaDto.NombreAyudante,
                ProveedorId = idProveedor,
                Cd = crearCitaDto.Cd,
                FechaCita = fechaCitaUtc,
                HoraCita = crearCitaDto.HoraCita,
                FechaSolicitud = TimeHelper.UtcNow(),
                TipoUnidad = crearCitaDto.TipoUnidad,
                Placas = crearCitaDto.Placas,
                LineaTransportista = crearCitaDto.LineaTransportista,
                Observaciones = crearCitaDto.Observaciones,
                Estado = EstadoCita.CREADA.ToString(),
                RegistradoPorId = usuario.Id
            };

            _context.Add(nuevaCita);
            await _context.SaveChangesAsync(ct);

            // genera trazabilidad operativa para la cita creada.
            await _actualizaResumenService.ActualizaResumen(nuevaCita, EstadoCita.CREADA);
            return nuevaCita.PublicId.ToString();
        }

        public async Task<Cita> ActualizarDatosCitaAsync(
            long citaId,
            long proveedorId,
            ActualizaDatosCitaDto dto,
            CancellationToken ct = default)
        {

            // Secuencia de validaciones.
            var ctx = new ActualizarDatosCitaContext { IdCita = citaId, ProveedorId = proveedorId, Dto = dto };
            var result = await _validatorActualizadatos.ValidateAsync(ctx, ct);
            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            var cita = ctx.State.cita;

            if (dto.Cd is not null) cita.Cd = dto.Cd.Trim().ToUpperInvariant();


            var fechaCitaUtc = DateTime.SpecifyKind(dto.FechaCita, DateTimeKind.Utc);
            cita.FechaCita = fechaCitaUtc;
            cita.HoraCita = dto.HoraCita;

            await _context.SaveChangesAsync(ct);
            return cita;
        }

        public async Task<(bool created, CitaLote entity)> RegistrarLoteAsync(
               string lote,
               long proveedorId,
               DateOnly? fechaCreacion = null,
               CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(lote))
                throw new ArgumentException("El lote es requerido.", nameof(lote));

            var existing = await _context.CitasLotes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Lote == lote && x.ProveedorId == proveedorId, ct);

            if (existing is not null)
                return (false, existing);

            var entity = new CitaLote
            {
                Lote = lote,
                ProveedorId = proveedorId,
                FechaCreacion = fechaCreacion ?? DateOnly.FromDateTime(DateTime.UtcNow)
            };

            _context.CitasLotes.Add(entity);

            await _context.SaveChangesAsync(ct);
            return (true, entity);
        }

        public async Task<List<CitaLote>> ObtenerLotesUltimasDosSemanasHastaMananaAsync(
            long? proveedorId = null,
            CancellationToken ct = default)
        {
            var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            // Rango: desde hace 14 días (incluido) hasta mañana (incluido)
            var inicio = todayUtc.AddDays(-14);
            var fin = todayUtc.AddDays(1);

            IQueryable<CitaLote> query = _context.CitasLotes.AsNoTracking()
                .Where(x => x.FechaCreacion >= inicio && x.FechaCreacion <= fin);

            if (proveedorId.HasValue)
                query = query.Where(x => x.ProveedorId == proveedorId.Value);

            return await query
                .OrderByDescending(x => x.FechaCreacion)
                .ThenBy(x => x.Lote)
                .ToListAsync(ct);
        }


        public async Task<List<CitaUuidFechasDto>> RecuperaCitasPorLoteAsync(
            string lote,
            long idProveedor,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(lote))
                throw new CitaException("El lote no debe ser vacío.");

            lote = lote.Trim();
            if (lote.Length > 20) lote = lote[..20];

            var citas = await _context.Citas
                .AsNoTracking()
                .Where(c => c.Lote == lote && c.ProveedorId == idProveedor)
                .OrderBy(c => c.CreadoEn)
                .Select(c => new CitaUuidFechasDto(
                    c.Id,
                    c.PublicId.ToString(),
                    c.FechaSolicitud,
                    c.FechaCita
                ))
                .ToListAsync(ct);

            return citas;
        }

        public async Task<ResultadoPaginado<CitaDto>> FiltrarAsync(
            CitasFiltroDto f,
            int pagina = 1,
            int tamanioPagina = 20,
            CancellationToken ct = default)
        {
            var query = _context.Set<Cita>()
                       .AsNoTracking()
                       .AsQueryable();

            if (f.ProveedorId.HasValue)
                query = query.Where(c => c.ProveedorId == f.ProveedorId.Value);

            if (f.IdCita.HasValue && f.IdCita > 0)
                query = query.Where(c => c.Id == f.IdCita.Value);

            if (!string.IsNullOrWhiteSpace(f.NumeroLote))
                query = query.Where(c => c.Lote == f.NumeroLote);

            if (!string.IsNullOrWhiteSpace(f.Folio))
                query = query.Where(c => c.Folio == f.Folio);

            if (!string.IsNullOrWhiteSpace(f.NumeroOrden))
                query = query.Where(c => c.Detalles.Any(d => d.Oc == f.NumeroOrden));

            if (f.SoloHoy)
            {
                var hoy = DateTime.Today;
                var manania = hoy.AddDays(1);
                query = query.Where(c => c.FechaCita >= hoy && c.FechaCita < manania);
            }
            else
            {
                
                if (f.FechaInicio.HasValue)
                {
                    f.FechaInicio = DateTime.SpecifyKind(f.FechaInicio ?? DateTime.Today, DateTimeKind.Utc);
                    query = query.Where(c => c.FechaCita >= f.FechaInicio.Value.Date);
                }

                if (f.FechaFin.HasValue)
                {
                    f.FechaFin = DateTime.SpecifyKind(f.FechaFin ?? DateTime.Today, DateTimeKind.Utc);
                    var finExclusivo = f.FechaFin.Value.Date.AddDays(1);
                    query = query.Where(c => c.FechaCita < finExclusivo);
                }
            }

            switch (f.Tipo)
            {
                case TipoConsultaCita.ConsultarAgendadas:
                    query = query.Where(c => c.Estado == "AGENDADA");
                    break;

                case TipoConsultaCita.ConsultarPendientesPorAutorizar:
                    query = query.Where(c => c.Estado == "SOLICITA_AUTORIZACION" || c.MarcadaParaSolicitarAutorizacion);
                    break;

                case TipoConsultaCita.ConsultarEntregadas:
                    query = query.Where(c => c.Estado == "ENTREGADA");
                    break;

                case TipoConsultaCita.ConsultarEntregadasConIncidencias:
                    query = query.Where(c => c.Estado == "ENTREGADA" && c.Incidencias.Any());
                    break;

                case TipoConsultaCita.ConsultarEntregadasSinIncidencias:
                    query = query.Where(c => c.Estado == "ENTREGADA" && !c.Incidencias.Any());
                    break;

                case TipoConsultaCita.ConsultarCanceladas:
                    query = query.Where(c => c.Estado == "CANCELADA");
                    break;

                case TipoConsultaCita.ConsultarSinFolio:
                    query = query.Where(c => string.IsNullOrEmpty(c.Folio));
                    break;

                case TipoConsultaCita.ConsultarLoteDeHoy:
                    {
                        var hoy = DateTime.Today;
                        var manania = hoy.AddDays(1);
                        query = query.Where(c => !string.IsNullOrEmpty(c.Lote) &&
                                         c.FechaCita >= hoy && c.FechaCita < manania);
                        break;
                    }

                case TipoConsultaCita.ConsultarLotePorFecha:
                    query = query.Where(c => !string.IsNullOrEmpty(c.Lote));
                    break;

                case TipoConsultaCita.Todas:
                default:
                    break;
            }

            tamanioPagina = Math.Clamp(tamanioPagina, 1, 500);
            var total = await query.CountAsync(ct);
            var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanioPagina));
            pagina = Math.Clamp(pagina, 1, totalPaginas);

            var elementos = await query
                .OrderByDescending(c => c.Id)
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .Select(ToDto())
                .ToListAsync(ct);

            return new ResultadoPaginado<CitaDto>
            {
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalElementos = total,
                Elementos = elementos
            };
        }

        private static Expression<Func<Cita, CitaDto>> ToDto() =>
            c => new CitaDto
            {
                Id = c.Id,
                Uuid = c.PublicId.ToString(),
                Lote = c.Lote,
                Folio = c.Folio,
                FechaCita = c.FechaCita,
                Cd = c.Cd,
                ProveedorId = c.ProveedorId,
                NombreSolicitante = c.NombreSolicitante,
                HoraCita = c.HoraCita,
                FechaSolicitud = c.FechaSolicitud,
                Estado = c.Estado,
                NombreChofer = c.NombreChofer,
                NombreAyudante = c.NombreAyudante,
                TipoUnidad = c.TipoUnidad,
                Placas = c.Placas,
                LineaTransportista = c.LineaTransportista,
                Observaciones = c.Observaciones,
                CreadoEn = c.CreadoEn,
                SolicitaAutorizacion = c.MarcadaParaSolicitarAutorizacion,
                NombreProveedor = c.Proveedor.Nombre,
                ClaveProveedor = c.Proveedor.Nombre,
                NombreCentroDistribucion = c.CentroDistribucion.Nombre
            };
    
    }
}

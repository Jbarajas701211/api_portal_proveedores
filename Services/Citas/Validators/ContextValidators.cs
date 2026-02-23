using ApiProveedores.Dto;
using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;
using System;
using System.Collections.Generic;

namespace ApiProveedores.Services.Citas.Validators
{
    public record RegistroCitaContext(
        CrearCitaDto Dto,
        long ProveedorId,
        DateTime NowUtc
    );


    public record CitaContext
    {
        public long IdCita { get; init; }
        public long? ProveedorId { get; init; }
        public CitaState State { get; init; } = new();

    }

    public record RegistrarDetalleCitaContext : CitaContext
    {
        public CrearCitaDetalleDto Dto { get; init; }
    }

    public record ActualizarDetalleCitaContext : CitaContext
    {
        public ActualizarCitaDetalleDto Dto { get; init; }
    }

    public record EliminarDetalleCitaContext : CitaContext
    {
        public EliminaCitaDetalleDto Dto { get; init; }
    }
    public record EliminarCitaContext : CitaContext { }
    public record GenerarFolioCitaContext : CitaContext { }
    public record SolicitarAutorizacionContext : CitaContext { }
    public record CancelacionContext : CitaContext { }
    public record EntregaValidatorContext : CitaContext {
        public EntregaDto Dto { get; init; }
    }
    public record IncidenciaValidatorContext : CitaContext
    {
        public IncidenciaDto Dto { get; init; }
    }

    public record IncidenciaMasivaValidatorContext : CitaContext
    {
        public IncidenciaMasivaDto Dto { get; init; }
    }

    public record IncidenciaSolicitaUrlValidatorContext : CitaContext
    {
        public SolicitaUrlSessionEvidenciasDto Dto { get; init; }
    }
    public record ActualizarDatosTransportisCitaContext : CitaContext
    {
        public TransporteDto Dto { get; init; }
    }

    public record ActualizarDatosCitaContext () : CitaContext
    {

        public ActualizaDatosCitaDto Dto { get; init; }
    }

    public sealed class CitaState
    {
        public Cita? cita { get; set; }
        public CitaDetalle? citaDetalle { get; set; }
        public Orden? orden { get; set; }
        public int cantidadDisponibleOC { get; set; }
        public CitaIncidencia? citaIncidencia { get; set; }
        public bool suceptibleParaSolicitarValidacion { get; set; } = false;
        public List<string> OrdenesBloqueadas { get; set; } = new();
    }

    

}

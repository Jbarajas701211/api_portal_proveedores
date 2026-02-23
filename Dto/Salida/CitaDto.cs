using System;

namespace ApiProveedores.Dto.Salida
{
    public class CitaDto
    {
        public long Id { get; set; }
        public string Uuid { get; set; }
        public string? Lote { get; set; }
        public string? Folio { get; set; }
        public DateTime FechaCita { get; set; }
        public string Cd { get; set; } = null!;
        public long ProveedorId { get; set; }
        public string NombreSolicitante { get; set; } = null!;
        public TimeSpan HoraCita { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; } = "CREADA";
        public string? NombreChofer { get; set; }
        public string? NombreAyudante { get; set; }
        public string? TipoUnidad { get; set; }
        public string? Placas { get; set; }
        public string? LineaTransportista { get; set; }
        public string? Observaciones { get; set; }
        public DateTime CreadoEn { get; set; }
        public bool SolicitaAutorizacion {  get; set; }
        public string NombreProveedor { get; set; }
        public string ClaveProveedor { get; set; }
        public string NombreCentroDistribucion { get; set; }
    }
}

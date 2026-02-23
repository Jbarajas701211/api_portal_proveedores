using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiProveedores.Models
{
    public class Cita
    {
        public long Id { get; set; }
        public string? Lote { get; set; }
        public string? Folio { get; set; }
        public DateTime FechaCita { get; set; }
        public string Cd { get; set; } = null!;
        public long ProveedorId { get; set; }
        public bool MarcadaParaSolicitarAutorizacion { get; set; }
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
        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
        public Proveedor Proveedor { get; set; } = null!;
        public CitaEntrega? Entrega { get; set; }
        public ICollection<CitaDetalle> Detalles { get; set; } = new List<CitaDetalle>();
        public ICollection<CitaIncidencia> Incidencias { get; set; } = new List<CitaIncidencia>();
        public int? RegistradoPorId { get; set; }
        public Usuario? RegistradoPor { get; set; }
        public Guid PublicId { get; set; }

        [ForeignKey(nameof(Cd))]
        public CentroDistribucion CentroDistribucion { get; set; }
    }

}

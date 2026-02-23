using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApiProveedores.Models
{
    public class CitaIncidencia
    {
        public long Id { get; set; }
        public long CitaId { get; set; }

        public string? Observacion { get; set; }
        public DateTime? RegistradoEn { get; set; }
        public string? RutaArchivo { get; set; }
        public bool ArchivoCargado { get; set; }
        public string? HashMasivo { get; set; }

        [JsonIgnore]
        public ICollection<CitaIncidenciaClave> Claves { get; set; } = new List<CitaIncidenciaClave>();


        [JsonIgnore] 
        public Cita Cita { get; set; } = null!;
    }

    public class CitaIncidenciaClave
    {
        public long Id { get; set; }
        public long CitaIncidenciaId { get; set; }
        public int ClaveInc { get; set; }

        public CitaIncidencia CitaIncidencia { get; set; } = null!;
        public CatalogoIncidencia CatalogoIncidencia { get; set; } = null!;
    }

    public class CatalogoIncidencia
    {
        public int Clave { get; set; }
        public string? Descripcion { get; set; }
    }
}

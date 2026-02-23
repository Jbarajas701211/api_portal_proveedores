using System;

namespace ApiProveedores.Dto.Entrada
{
    public class CrearCitaDto
    {
        public string Lote { get; set; }
        public string Cd { get; set; } = null!;
        public DateTime FechaCita { get; set; }
        public TimeSpan HoraCita { get; set; }
        public string NombreChofer { get; set; }
        public string NombreAyudante { get; set; }
        public string TipoUnidad { get; set; }
        public string Placas { get; set; }
        public string LineaTransportista { get; set; }
        public string Observaciones { get; set; }
    }

    public class ActualizaDatosCitaDto
    {
        public string Cd { get; set; } = null!;
        public DateTime FechaCita { get; set; }
        public TimeSpan HoraCita { get; set; }
    }
}

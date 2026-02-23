using System;

namespace ApiProveedores.Models
{
    public class CapacidadResumenCd
    {
        public string Cd { get; set; } = null!;
        public string Origen { get; set; } = null!;
        public DateTime Fecha { get; set; }

        public int CapacidadMaxima { get; set; }
        public int CapacidadUtilizada { get; set; }
        public int CapacidadDisponible { get; set; }
        public DateTime ActualizadoEn { get; set; }

        public CapacidadCdOrigen? CapacidadCdOrigen { get; set; }
    }

}

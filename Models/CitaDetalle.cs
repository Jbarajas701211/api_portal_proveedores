using System;

namespace ApiProveedores.Models
{
    public class CitaDetalle
    {
        public long CitaId { get; set; }
        public string Oc { get; set; } = null!;
        public string Origen { get; set; } = null!;
        public string ClaveAlmacen { get; set; } = null!;
        public int CantidadPorCita { get; set; }
        public int CantidadTotal { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime RegistradoEn { get; set; }
        public Cita Cita { get; set; } = null!;
    }
}

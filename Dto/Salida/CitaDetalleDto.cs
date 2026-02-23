using System;

namespace ApiProveedores.Dto.Salida
{
    public class CitaDetalleDto
    {
        public long CitaId { get; set; }
        public string Oc { get; set; } = null!;
        public string Origen { get; set; } = null!;
        public int CantidadPorCita { get; set; }
        public DateTime RegistradoEn { get; set; }
        public int CantidadTotal { get; set; }
        public DateTime FechaVencimiento { get; set; }
    }
}

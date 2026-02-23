using System;

namespace ApiProveedores.Models
{
    public class Devolucion
    {
        public long Id { get; set; }
        public long ProveedorId { get; set; }
        public int? Cantidad { get; set; }
        public string? NumeroRtv { get; set; }
        public DateOnly? FechaRecoleccion { get; set; }
        public int CreadoPorId { get; set; }

        public DateTime CreadoEn { get; set; }
        public DateTime ActualizadoEn { get; set; }
        public Proveedor? Proveedor { get; set; }
        public Usuario? CreadoPor { get; set; }
    }

}

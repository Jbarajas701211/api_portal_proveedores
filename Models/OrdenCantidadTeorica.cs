using System;

namespace ApiProveedores.Models
{
    public class OrdenCantidadTeorica
    {
        public string Oc { get; set; } = null!;
        public int CantidadTeorica { get; set; }
        public int CantidadTotal { get; set; }
        public int CantidadEntregada { get; set; }
        public DateTime RegistradoEn { get; set; }
        public DateTime ActualizadoEn { get; set; }
    }
}

using System;

namespace ApiProveedores.Models
{
    public class DiaNoLaborable
    {
        public long Id { get; set; }

        public DateTime Fecha { get; set; }

        public string Descripcion { get; set; }

        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    }
}

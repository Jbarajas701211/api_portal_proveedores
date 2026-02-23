using System;
using System.Collections.Generic;

namespace ApiProveedores.Models
{
    public class CapacidadCdOrigen
    {
        public long Id { get; set; }
        public string Cd { get; set; } = null!;
        public string Origen { get; set; } = null!;
        public int CapacidadMaxima { get; set; }
        public DateTime RegistradoEn { get; set; }

        public ICollection<CapacidadResumenCd>? Resumenes { get; set; }
        public ICollection<CapacidadUso>? Usos { get; set; }
    }

}

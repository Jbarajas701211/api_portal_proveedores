using System;

namespace ApiProveedores.Dto.Entrada
{
    public class KpiProveedoresRequest
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public string ClaveProveedor { get; set; }
    }

}

using System;

namespace ApiProveedores.Models
{
    public class Tienda
    {
        public int CcCntrCsto { get; set; }
        public string? CcScrs { get; set; }
        public DateTime CreadoEn { get; set; }
        public string? OFlag { get; set; }
        public string? OExMsg { get; set; }
        public DateTime? ODttm { get; set; }
    }
}

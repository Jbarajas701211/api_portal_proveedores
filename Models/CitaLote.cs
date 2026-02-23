using System;

namespace ApiProveedores.Models
{
    public class CitaLote
    {
        public string? Lote { get; set; }
        public long ProveedorId { get; set; }
        public DateOnly FechaCreacion { get; set; }
    }
}

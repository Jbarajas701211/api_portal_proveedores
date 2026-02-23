using ApiProveedores.Models;
using System;

namespace ApiProveedores.Dto.Entrada
{

    public class FallaMasivaDto
    {
        public long[] CitaIds { get; set; }
        public string? Notas { get; set; }
    }

    public class EntregaDto
    {
        public long CitaId { get; set; }
        public DateOnly FechaEntrega { get; set; }
        public TimeOnly HoraRecepcion { get; set; }
        public int CantidadEntregada { get; set; }
        public EntregaEstatus Estatus { get; set; }
        public string? Anden { get; set; }
        public string? Acuse { get; set; }
        public string? Notas { get; set; }
    }
}

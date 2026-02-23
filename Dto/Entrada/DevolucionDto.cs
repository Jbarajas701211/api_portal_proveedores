using System;

namespace ApiProveedores.Dto.Entrada
{
    public sealed class RegistrarDevolucionDto
    {
        public long ProveedorId { get; set; }
        public int? Cantidad { get; set; }
        public string? NumeroRtv { get; set; }
        public DateOnly? FechaRecoleccion { get; set; }
    }

    public sealed class ActualizaDevolucionDto
    {
        public long IdDevolucion { get; set; }
        public int? Cantidad { get; set; }
        public string? NumeroRtv { get; set; }
        public DateOnly? FechaRecoleccion { get; set; }
    }
}

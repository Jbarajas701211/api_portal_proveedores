using System;

namespace ApiProveedores.Dto.Salida
{
    public class DevolucionDto
    {
        public long Id { get; set; }
        public long ProveedorId { get; set; }
        public int? Cantidad { get; set; }
        public string? NumeroRtv { get; set; }
        public DateOnly? FechaRecoleccion { get; set; }

        public DateTime CreadoEn { get; set; }
        public DateTime ActualizadoEn { get; set; }

        public ProveedorMiniDto? Proveedor { get; set; }
        public UsuarioMiniDto? CreadoPor { get; set; }
    }


    public class ProveedorMiniDto
    {
        public long Id { get; set; }
        public string? Nombre { get; set; }
    }

    public class UsuarioMiniDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
    }
}

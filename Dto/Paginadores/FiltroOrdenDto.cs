using System;

namespace ApiProveedores.Dto
{
    public class FiltroOrdenDto
    {
        public string? NumeroOrden { get; set; }
        public string? Proveedor { get; set; }
        public string? CentroDistribucion { get; set; }
        public string? TipoOrden { get; set; }
        public string? Estatus { get; set; }
        public DateTime? FechaRegistroInicio { get; set; }
        public DateTime? FechaRegistroFin { get; set; }
        public DateTime? FechaVencimientoInicio { get; set; }
        public DateTime? FechaVencimientoFin { get; set; }
        public int Pagina { get; set; } = 1;
        public int RegistrosPorPagina { get; set; } = 10;
    }

    public class FiltroDetalleOrdenDto
    {
        public string? NumeroOrden { get; set; }
        public string? Proveedor { get; set; }
        public int Pagina { get; set; } = 1;
        public int RegistrosPorPagina { get; set; } = 10;
    }
}

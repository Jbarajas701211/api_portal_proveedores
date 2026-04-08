namespace ApiProveedores.Models
{
    public class RecepcionDetalle
    {
        public long IdDetalle { get; set; }

        public long IdRecepcion { get; set; }

        public string? Linea { get; set; }

        public string? ItemId { get; set; }
        public string? ItemNombre { get; set; }
        public string? Descripcion { get; set; }

        public string? UnidadId { get; set; }
        public string? UnidadNombre { get; set; }

        public decimal? Cantidad { get; set; }
        public decimal? PrecioUnitario { get; set; }

        public decimal? Subtotal { get; set; }
        public decimal? Total { get; set; }

        
        public Recepcion Recepcion { get; set; } = null!;
    }
}

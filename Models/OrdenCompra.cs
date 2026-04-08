namespace ApiProveedores.Models
{
    public class OrdenCompra
    {
        public long IdOrdenCompra { get; set; }

        public string ErpOrigen { get; set; } = null!;
        public string IdExterno { get; set; } = null!;
        public string? Folio { get; set; }

        public DateTime? FechaOc { get; set; }
        public string? Moneda { get; set; }
        public decimal? Total { get; set; }

        public string? ProveedorId { get; set; }
        public string? ProveedorNombre { get; set; }
        public string? ProveedorRfc { get; set; }

        public string? Sociedad { get; set; }
        public string? Subsidiaria { get; set; }

        public ICollection<Recepcion> Recepciones { get; set; } = new List<Recepcion>();
    }
}

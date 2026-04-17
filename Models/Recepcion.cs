using System;
using System.Collections.Generic;

namespace ApiProveedores.Models
{
    public class Recepcion
    {
        public long IdRecepcion { get; set; }

        public long IdOrdenCompra { get; set; }

        public string ErpOrigen { get; set; } = null!;
        public string IdExterno { get; set; } = null!;
        public string? Folio { get; set; }

        public DateTime? FechaRecepcion { get; set; }
        public DateTime? FechaContabilizacion { get; set; }

        public string? Moneda { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Total { get; set; }

        public string? Estado { get; set; }
        public string? UsuarioCreacion { get; set; }

        public string? ProveedorId { get; set; }
        public string? ProveedorNombre { get; set; }
        public string? ProveedorRfc { get; set; }

        public string? Sociedad { get; set; }
        public string? Centro { get; set; }
        public decimal? Cantidad { get; set; }

        public OrdenCompra OrdenCompra { get; set; } = null!;
        public ICollection<RecepcionDetalle> Detalles { get; set; } = new List<RecepcionDetalle>();

        public ICollection<FacturaRecepcion> FacturaRecepcion { get; set; } = new List<FacturaRecepcion>();
    }
}

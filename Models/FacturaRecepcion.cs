using System.ComponentModel.DataAnnotations.Schema;

namespace ApiProveedores.Models
{
    public class FacturaRecepcion
    {
        public int Id { get; set; }
        public long RecepcionId { get; set; }
        public Recepcion Recepcion { get; set; }
        public long FacturaId { get; set; }
        public Factura.Factura Factura { get; set; }
    }
}

using System;

namespace ApiProveedores.Models
{
    public class DetalleOrden
    {
        public string Nopedido { get; set; } = default!;
        public string Idarticulo { get; set; } = default!;
        public int Iddetapedi { get; set; }

        public string Upc { get; set; } = default!;
        public decimal Cantidad { get; set; }
        public string Cvetienda { get; set; } = default!;
        public decimal Costo { get; set; }

        public string Descrip { get; set; } = default!;
        public string Modelo { get; set; } = default!;
        public string Color { get; set; } = default!;
        public string Talla { get; set; } = default!;
        public string Marca { get; set; } = default!;

        public int? Purchaseord { get; set; }
    }

}

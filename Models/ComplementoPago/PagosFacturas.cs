namespace ApiProveedores.Models.ComplementoPago
{
    public class PagosFacturas
    {
        public int Id { get; set; }
        public int PagoId { get; set; }
        public string UuidFactura { get; set; }
        public string Serie { get; set; }
        public string Folio { get; set; }
        public int NumeroParcialidad { get; set; }
        public decimal ImporteSaldoAnterior { get; set; }
        public decimal ImportePagado { get; set; }
        public decimal ImporteSaldoInsoluto { get; set; }

    }
}

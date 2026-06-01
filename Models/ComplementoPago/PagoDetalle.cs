namespace ApiProveedores.Models.ComplementoPago
{
    public class PagoDetalle
    {
        public int Id { get; set; }
        public int PagoCfdiId { get; set; }
        public DateTime FechaPago { get; set; }
        public string FormaPago { get; set; }
        public string Moneda { get; set; }
        public decimal TipoCambio { get; set; }
        public decimal Monto { get; set; }
        public string NumeroOperacion { get; set; }
        public string BancoOrdenante { get; set; }
        public string CuentaOrdenante { get; set; }
        public string CuentaBeneficiario { get; set; }

    }
}

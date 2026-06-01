namespace ApiProveedores.Models.ComplementoPago
{
    public class PagoCfdi
    {
        public int Id { get; set; }
        public string Uuid { get; set; }
        public string Serie { get; set; }
        public string Folio { get; set; }
        public DateTime Fecha { get; set; }
        public string RfcEmisor { get; set; }
        public string NombreEmisor { get; set; }
        public string RfcReceptor { get; set; }
        public string NombreReceptor { get; set; }
        public decimal Total { get; set; }
        public string XmlOriginal { get; set; }
        public DateTime FechaAlta { get; set; }

    }
}

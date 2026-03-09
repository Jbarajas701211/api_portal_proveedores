namespace ApiProveedores.Dto.Entrada
{
    public class ProveedorDto
    {
        public long Id { get; set; }
        public string ClaveProveedor { get; set; }
        public string NombreProveedor { get; set; }
        public string Rfc { get; set; }
        public bool Estatus { get; set; }
        public decimal Sobrante { get; set; }
        public decimal PorcentajeSobrante { get; set; }
        public decimal Faltante { get; set; }
        public decimal PorcentajeFaltante { get; set; }
        public bool AplicarTolerancia { get; set; }
        public int IdCategoria { get; set; }
        public bool AccredorSinXml { get; set; }
        public bool AplicarToleranciaCategoria { get; set; }
        public string Email { get; set; }
        public string DocumentoFiscal { get; set; }
        public bool Factura { get; set; }
        public bool Recepcion { get; set; }
        public string Origen { get; set; }
        public string RazonSocial { get; set; }
    }
}

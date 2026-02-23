namespace ApiProveedores.Dto.Salida
{
    public class OrdenDto
    {
        public string Nopedido { get; set; }
        public string Fechapedido { get; set; }
        public string Fechavenci { get; set; }
        public string Origen { get; set; }
        public string Cveprov { get; set; }
        public string Proveedor { get; set; }
        public short Status { get; set; }
        public string Estatus { get; set; }
        public string Cd { get; set; }
        public string CdDesc { get; set; }
        public decimal? Importe { get; set; }
        public decimal Comprador { get; set; }
        public decimal? Cantitotal { get; set; }
        public string Unnego { get; set; }
        public decimal Cvecateg { get; set; }
        public short? Notacanc { get; set; }
        public string Asn { get; set; }
        public string Basico { get; set; }
        public string TipoOrden { get; set; }
        public string CitaInd { get; set; }
        public string OFlag { get; set; }
        public string OExMsg { get; set; }

        public string ODttm { get; set; }

        public string? ClaveAlmacen { get; set; }

        public int CantidadTeorica { get; set; }
        public int CantidadEntregada { get; set; }
        public int CantidadFaltante { get; set; }
        public string NombreCentroDistribucion { get; set; }
    }

}

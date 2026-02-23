namespace ApiProveedores.Models
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Orden
    {
        public string Nopedido { get; set; }
        public DateTime Fechapedido { get; set; }
        public DateTime Fechavenci { get; set; }
        public string Origen { get; set; }
        public string Cveprov { get; set; }
        public short Status { get; set; }
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
        public DateTime? ODttm { get; set; }
        public bool? Bloqueado { get; set; }
        public string CveAlmacen { get; set; }

        public int CantidadTeorica { get; set; }
        public int CantidadEntregada { get; set; }
        public int CantidadFaltante { get; set; }

        [ForeignKey(nameof(Cd))]
        public CentroDistribucion CentroDistribucion { get; set; }
    }

    public class OrdenSeguimiento
    {
        public long Id { get; set; }
        public string Nopedido { get; set; } = null!;
        public string Evento { get; set; } = null!;
        public string? Descripcion { get; set; }
        public DateTime RegistradoEn { get; set; }
        public long? UsuarioModifico { get; set; }
        public bool EstadoActivo { get; set; }
    }
}

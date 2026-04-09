using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiProveedores.Models.Factura
{
    public class Factura
    {
        [Key]
        [Column("id_factura")]
        public long IdFactura { get; set; }

        [Column("id_proveedor")]
        public long IdProveedor { get; set; }

        [Column("id_empresa")]
        public long IdEmpresa { get; set; }

        [MaxLength(50)]
        [Column("tipo_de_comprobante")]
        public string? TipoDeComprobante { get; set; }

        [MaxLength(50)]
        [Column("estatus_factura")]
        public string? EstatusFactura { get; set; }

        [MaxLength(50)]
        [Column("folio_origen")]
        public string? FolioOrigen { get; set; }

        [MaxLength(50)]
        [Column("folio")]
        public string? Folio { get; set; }

        [MaxLength(50)]
        [Column("serie")]
        public string? Serie { get; set; }

        [MaxLength(50)]
        [Column("uuid")]
        public string? Uuid { get; set; }

        [MaxLength(150)]
        [Column("motivo")]
        public string? Motivo { get; set; }

        [Column("hay_evidencia")]
        public bool HayEvidencia { get; set; }

        [Column("fecha_alta")]
        public DateTime? FechaAlta { get; set; }

        [Column("fecha_factura")]
        public DateTime? FechaFactura { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("cd_total")]
        public decimal CdTotal { get; set; }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("monto_de_recepcion")]
        public decimal MontoDeRecepcion { get; set; }

        [MaxLength(150)]
        [Column("correo_electronico")]
        public string? CorreoElectronico { get; set; }

        [Column("xml", TypeName = "text")]
        public string? Xml { get; set; }

        [Column("representacion_grafica", TypeName = "text")]
        public string? RepresentacionGrafica { get; set; }

        [MaxLength(100)]
        [Column("unidad_negocio")]
        public string? UnidadNegocio { get; set; }

        [MaxLength(50)]
        [Column("no_orden_compra")]
        public string? NoOrdenCompra { get; set; }

        [MaxLength(50)]
        [Column("no_recepcion")]
        public string? NoRecepcion { get; set; }

        [MaxLength(10)]
        [Column("version_cfdi")]
        public string? VersionCfdi { get; set; }

        [Column("ieps")]
        public decimal Ieps { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; }

        [Column("iva")]
        public decimal Iva { get; set; }

        [MaxLength(50)]
        [Column("folio_erp")]
        public string? FolioErp { get; set; }

        [Column("fecha_contabilizacion")]
        public DateTime? FechaContabilizacion { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_modificacion")]
        public DateTime? FechaModificacion { get; set; }

        public ICollection<FacturaRecepcion> FacturaRecepcion { get; set; } = new List<FacturaRecepcion>();
    }
}

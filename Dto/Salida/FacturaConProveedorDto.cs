using ApiProveedores.Models.Enum;

public class FacturaConProveedorDto
{
    public long IdFactura { get; set; }
    public long IdProveedor { get; set; }
    public string? NombreProveedor { get; set; }
    public string? TipoDeComprobante { get; set; }
    public EstatusFacturaEnum? EstatusFactura { get; set; }
    public string? FolioOrigen { get; set; }
    public string? Folio { get; set; }
    public string? Serie { get; set; }
    public string? Uuid { get; set; }
    public string? Motivo { get; set; }
    public bool HayEvidencia { get; set; }
    public DateTime? FechaAlta { get; set; }
    public DateTime? FechaFactura { get; set; }
    public decimal Subtotal { get; set; }
    public decimal CdTotal { get; set; }
    public decimal Total { get; set; }
    public decimal MontoDeRecepcion { get; set; }
    public string? CorreoElectronico { get; set; }
    public string? Xml { get; set; }
    public string? RepresentacionGrafica { get; set; }
    public string? UnidadNegocio { get; set; }
    public string? NoOrdenCompra { get; set; }
    public string? NoRecepcion { get; set; }
    public string? VersionCfdi { get; set; }
    public decimal Ieps { get; set; }
    public DateTime? FechaRegistro { get; set; }
    public decimal Iva { get; set; }
    public string? FolioErp { get; set; }
    public DateTime? FechaContabilizacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}

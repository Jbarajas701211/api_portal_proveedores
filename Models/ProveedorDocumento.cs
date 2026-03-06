namespace ApiProveedores.Models
{
    public class ProveedorDocumento
    {
        public int IdRelacionPD { get; set; }
        public int IdProveedor { get; set; }
        public int IdDocumento { get; set; }
        public bool Opcional { get; set; }
        public Proveedor Proveedor { get; set; }
        public Documento Documento { get; set; }

    }
}

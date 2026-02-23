namespace ApiProveedores.Models
{
    public class ProveedorEmpresa
    {
        public int IdRelacionPE { get; set; }
        public long IdProveedor { get; set; }
        public Proveedor Proveedor { get; set; }    // <-- navegación
        public int IdEmpresa { get; set; }
        public Empresa Empresa { get; set; }
    }
}

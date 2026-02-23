namespace ApiProveedores.Models
{
    public class UsuarioEmpresa
    {
        public int IdRelacionUE { get; set; }
        public int IdUsuario { get; set; }
        public Usuario Usuario { get; set; }
        public int IdEmpresa { get; set; }
        public Empresa Empresa { get; set; }
    }
}

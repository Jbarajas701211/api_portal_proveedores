namespace ApiProveedores.Models
{
    public class UsuarioRol
    {
        public long IdRelacionUr { get; set; }
        public int IdUsuario { get; set; }
        public Usuario Usuario { get; set; }
        public long IdRol { get; set; }
        public Rol Rol { get; set; }
    }
}

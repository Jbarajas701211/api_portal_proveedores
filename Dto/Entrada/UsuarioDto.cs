namespace ApiProveedores.Dto.Entrada
{
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Nombre { get; set; }
        public string? ApellidoPaterno { get; set; }
        public string? ApellidoMaterno { get; set; }
        public string? Rol { get; set; }
        public bool Activo { get; set; }
        public bool Habilitado { get; set; }
        public string? Usuario { get; set; }
        public string? RfcProveedor { get; set; }
    }

    public class HabilitarUsuarioDto
    {
        public int IdUsuario { get; set; }
        public bool Habilitado { get; set; }
    }
}

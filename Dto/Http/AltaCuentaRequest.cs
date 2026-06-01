namespace ApiProveedores.Dto.Http
{
    using System.ComponentModel.DataAnnotations;

    public enum RolUsuario
    {
        PROVEEDOR = 2,
        ADMIN = 1
    }

    public class AltaCuentaRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string? Nombre { get; set; }
        [Required(ErrorMessage = "El apellido paterno es obligatorio.")]
        public string? ApellidoPaterno { get; set; }
        public string? ApellidoMaterno { get; set; }
        [Required(ErrorMessage = "El Usuario es obligatorio.")]
        public string? Usuario { get; set; }
        public string? Password { get; set; }
        public bool Estatus { get; set; }

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
        public string Email { get; set; } = string.Empty;

        [EnumDataType(typeof(RolUsuario), ErrorMessage = "El rol no es válido.")]
        public RolUsuario? Rol { get; set; }
        public string? RfcProveedor { get; set; }
    }

}

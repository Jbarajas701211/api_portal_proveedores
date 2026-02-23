namespace ApiProveedores.Dto.Http
{
    using System.ComponentModel.DataAnnotations;

    public enum RolUsuario
    {
        PROVEEDOR,
        ONEST,
        LOGISTICA
    }

    public class AltaCuentaRequest
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
        public string Email { get; set; } = string.Empty;

        public int ProveedorId { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio.")]
        [EnumDataType(typeof(RolUsuario), ErrorMessage = "El rol no es válido.")]
        public RolUsuario Rol { get; set; }
    }

}

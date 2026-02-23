
using System.ComponentModel.DataAnnotations;

namespace ApiProveedores.Dto.Http;

public class RegistroPasswordRequest
{
    [Required(ErrorMessage = "El password es obligatorio.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmacion password es obligatorio.")]
    public string Confirmacion { get; set; } = string.Empty;

}

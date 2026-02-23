
using System.ComponentModel.DataAnnotations;

namespace ApiProveedores.Dto.Http;

public class RecuperacionRequest
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    public string Email { get; set; } = string.Empty;
}

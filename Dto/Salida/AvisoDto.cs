using System.ComponentModel.DataAnnotations;

namespace ApiProveedores.Dto.Salida
{
    public class AvisoDto
    {
        public int? IdAviso { get; set; }
        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [StringLength(50, ErrorMessage = "La categoría no puede exceder los 50 caracteres.")]
        public string Categoria { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es obligatorio.")]
        public string Mensaje { get; set; } = string.Empty;

        public bool Estatus { get; set; } = true;

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateTime FechaInicioAviso { get; set; }

        public DateTime FechaFinalAviso { get; set; }
    }
}

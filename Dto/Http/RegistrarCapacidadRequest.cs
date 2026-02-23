using System;
using System.ComponentModel.DataAnnotations;
namespace ApiProveedores.Dto.Http
{
    public class RegistrarCapacidadDto
    {
        [Required(ErrorMessage = "El campo 'Cd' es obligatorio.")]
        [StringLength(1, ErrorMessage = "El campo 'Cd' debe tener exactamente 1 carácter.")]
        public string Cd { get; set; } = null!;

        [Required(ErrorMessage = "El campo 'Origen' es obligatorio.")]
        [MaxLength(10, ErrorMessage = "El campo 'Origen' no puede tener más de 10 caracteres.")]
        public string Origen { get; set; } = null!;

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        public DateTime Fecha { get; set; }

        public int Cantidad { get; set; }
    }

}

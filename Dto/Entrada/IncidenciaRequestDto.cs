using System.ComponentModel.DataAnnotations;

namespace ApiProveedores.Dto.Entrada
{
    public class IncidenciaRequestDto
    {
        public int Clave { get; set; }
        [Required]
        public string Descripcion { get; set; }
    }
}

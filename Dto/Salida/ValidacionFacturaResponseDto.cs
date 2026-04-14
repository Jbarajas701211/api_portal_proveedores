using ApiProveedores.Models.Enum;
using System.Security.Principal;

namespace ApiProveedores.Dto.Salida
{
    public class ValidacionFacturaResponseDto: ApiResponseDto
    {
        public string? ProcesoId { get; set; }
        public decimal Diferencia { get; set; }
        public TipoAccionSiguientejEnum Accion { get; set; }
    }
}

using System.Collections.Generic;

namespace ApiProveedores.Dto.Salida
{
    public class IncidenciaMasivaResultadoDto
    {
        public string HashMasivo { get; set; } = default!;
        public List<long> IncidenciasIds { get; set; } = new();
    }
}

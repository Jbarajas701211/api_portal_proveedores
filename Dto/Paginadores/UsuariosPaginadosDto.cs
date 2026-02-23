using ApiProveedores.Dto.Entrada;
using System.Collections.Generic;

namespace ApiProveedores.Dto.Paginadores
{
    public class UsuariosPaginadosDto
    {
        public int Total { get; set; }
        public List<UsuarioDto> Resultados { get; set; } = new();
    }

}

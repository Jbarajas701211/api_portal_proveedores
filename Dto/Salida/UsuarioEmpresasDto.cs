using ApiProveedores.Dto.Entrada;

namespace ApiProveedores.Dto.Salida
{
    public class UsuarioEmpresasDto : UsuarioDto
    {
        public int[] Empresas { get; set; }
    }
}

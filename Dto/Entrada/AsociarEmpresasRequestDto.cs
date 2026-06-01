namespace ApiProveedores.Dto.Entrada
{
    public class AsociarEmpresasRequestDto
    {
        public int IdUsuario { get; set; }
        public int[] IdEmpresas { get; set; }
    }
}

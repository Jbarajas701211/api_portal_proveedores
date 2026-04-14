namespace ApiProveedores.Dto
{
    public class EmpresaDto
    {
        public int IdEmpresa { get; set; }
        public string? Nombre { get; set; }
        public string? Rfc { get; set; }
        public bool Estatus { get; set; }
        public string? Unidad { get; set; }
    }
}

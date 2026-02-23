namespace ApiProveedores.Dto.Salida
{
    public sealed class CentroDistribucionDto
    {
        public string Clave { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public bool Activo { get; set; }
    }
}

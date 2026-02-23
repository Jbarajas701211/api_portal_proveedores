namespace ApiProveedores.Models
{
    public class CatalogoOrigenCapacidad
    {
        public string Clave { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
        public bool Activo { get; set; } = true;
        public string? ClaveOrigen { get; set; }
    }
}

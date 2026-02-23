namespace ApiProveedores.Dto.Entrada
{
    public class CapacidadBaseDto
    {
        public string Cd { get; set; } = null!;
        public string Origen { get; set; } = null!;
        public string OrigenDescripcion { get; set; }
        public int CapacidadMaxima { get; set; }
        public int CapacidadUtilizada { get; set; }
        public int CapacidadDisponible { get; set; }
    }
}

namespace ApiProveedores.Models
{
    public class Aviso
    {
        public int IdAviso { get; set; }
        public string? Categoria { get; set; }
        public string? Mensaje { get; set; }
        public bool Estatus { get; set; }
        public DateTime FechaInicioAviso { get; set; }
        public DateTime FechaFinalAviso { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}

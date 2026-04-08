namespace ApiProveedores.Dto
{
    public class RecepcionResponseDto
    {
        public long IdRecepcion { get; set; }
        public DateTime? Fecha { get; set; }
        public decimal Cantidad { get; set; }
        public decimal? Monto { get; set; }
    }
}

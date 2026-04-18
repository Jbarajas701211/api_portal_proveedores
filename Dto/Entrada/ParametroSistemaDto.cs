namespace ApiProveedores.Dto.Entrada
{
    public class ParametroSistemaDto
    {
        public int IdParametro { get; set; }
        public string Valor { get; set; } = string.Empty;
        public string Descripcion { get; set; }
        public string Codigo { get; set; }
        public string UnidadMedida { get; set; }
        public bool Notificacion { get; set; }
        public DateTime Modificado { get; set; }
        public bool Estatus { get; set; }
    }

    public class ActualizarValorParametroDto
    {
        public string Clave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }
}

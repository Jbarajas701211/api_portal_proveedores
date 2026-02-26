using System;

namespace ApiProveedores.Models
{
    public class ParametroSistema
    {
        public int IdParametro { get; set; }
        public string Valor { get; set; }
        public string? Descripcion { get; set; }
        public string UnidadMedida { get; set; }
        public bool Notificacion { get; set; }
        public DateTime Modificacion { get; set; }
        public int IdUsuario { get; set; }
        public Usuario Usuario { get; set; }
    }
}

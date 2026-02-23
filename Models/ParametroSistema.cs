using System;

namespace ApiProveedores.Models
{
    public class ParametroSistema
    {
        public string Clave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime ActualizadoEn { get; set; }
    }
}

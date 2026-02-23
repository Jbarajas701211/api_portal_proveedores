using System;

namespace ApiProveedores.Dto.Entrada
{
    public class NotificacionDto
    {
        public long Id { get; set; }
        public string Titulo { get; set; }
        public string Tag { get; set; }
        public string Detalle { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public bool Leida { get; set; }
        public DateTime? LeidaEn { get; set; }
    }

}

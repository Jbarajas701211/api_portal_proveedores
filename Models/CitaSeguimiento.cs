using System;

namespace ApiProveedores.Models
{
    public class CitaSeguimiento
    {
        public long Id { get; set; }
        public long CitaId { get; set; }
        public string Evento { get; set; } = null!;
        public bool Notificado { get; set; }
        public bool EstadoActivo { get; set; }
        public DateTime? FechaNotificacion { get; set; }
        public long? UsuarioModifico { get; set; }
        public DateTime RegistradoEn { get; set; }
        public Cita? Cita { get; set; }
        public bool ConIncidencias { get; set; }
    }

}

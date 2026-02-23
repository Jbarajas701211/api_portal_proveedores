using System;
using System.Collections.Generic;

namespace ApiProveedores.Models
{

    public class NotificacionUsuario
    {
        public long Id { get; set; }
        public long NotificacionId { get; set; }
        public int UsuarioId { get; set; }
        public bool Leida { get; set; } = false;
        public DateTime? LeidaEn { get; set; }

        public Notificacion Notificacion { get; set; } = null!;
        public Usuario Usuario { get; set; } = null!;
    }

    public class Notificacion
    {
        public long Id { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string Titulo { get; set; } = null!;
        public string Tag { get; set; } = null!;
        public string? Detalle { get; set; }
        public DateTime CreadoEn { get; set; }
        public string? MetaData { get; set; }
        public ICollection<NotificacionUsuario> NotificacionesUsuarios { get; set; } = new List<NotificacionUsuario>();
    }
}

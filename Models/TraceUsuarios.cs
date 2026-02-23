namespace ApiProveedores.Models
{
    using System;
    using static AuthService;

    public class TraceUsuario
    {
        public long Id { get; set; }

        public int IdUsuario { get; set; }

        public EventoUsuario Evento { get; set; }

        public string Descripcion { get; set; }

        public DateTimeOffset RegistradoEn { get; set; }

        public Usuario Usuario { get; set; }
    }

}

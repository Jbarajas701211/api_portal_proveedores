using System;
using ApiProveedores.Models;

namespace ApiProveedores.Dto.Auth
{
    public class RefreshToken
    {
        public long Id { get; set; }

        public int UsuarioId { get; set; }
        public string Token { get; set; }
        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
        public DateTime ExpiraEn { get; set; }
        public DateTime? RevocadoEn { get; set; }
        public string ReemplazadoPor { get; set; }
        public bool EstaActivo => RevocadoEn == null && ExpiraEn > DateTime.UtcNow;

        public Usuario Usuario { get; set; }
    }
}

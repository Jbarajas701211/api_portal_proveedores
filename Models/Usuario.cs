
using System;
using System.Collections.Generic;
using ApiProveedores.Dto.Auth;

namespace ApiProveedores.Models;

public class Usuario
{
    public int IdUsuario { get; set; }
    public string usuario { get; set; }
    public string Password { get; set; }
    public string Nombre { get; set; }
    public string ApellidoPaterno { get; set; }
    public string ApellidoMaterno { get; set; }
    public string CorreoElectronico { get; set; }
    public bool Estatus { get; set; }

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
        public ICollection<TraceUsuario> TraceUsuarios { get; set; } = new List<TraceUsuario>();
    public ICollection<UsuarioEmpresa> UsuarioEmpresas { get; set; } = new List<UsuarioEmpresa>();

    public ICollection<RefreshToken> RefreshTokens { get; set; }

    //public long? ProveedorId { get; set; }
    //public string Rol { get; set; }
    //public string CodigoAutorizacion { get; set; }
    //public int IntentosFallidos { get; set; }
    //public DateTime? BloqueadoEn { get; set; }
    //public string Secret2FA { get; set; }
    //public bool Activo { get; set; }
    //public bool Habilitado { get; set; }
    //public ICollection<TraceUsuario> TraceUsuarios { get; set; }
    //public Proveedor Proveedor { get; set; }
    //public ICollection<RefreshToken> RefreshTokens { get; set; }
    //public ICollection<NotificacionUsuario> NotificacionesUsuarios { get; set; } = new List<NotificacionUsuario>();


}

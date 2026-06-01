
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
    public string? CodigoActivacion { get; set; }
    public string RfcProveedor { get; set; }
    public Proveedor? Proveedor { get; set; }

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public ICollection<TraceUsuario> TraceUsuarios { get; set; } = new List<TraceUsuario>();
    public ICollection<UsuarioEmpresa> UsuarioEmpresas { get; set; } = new List<UsuarioEmpresa>();

    public ICollection<RefreshToken> RefreshTokens { get; set; }

    public ICollection<NotificacionUsuario> NotificacionesUsuarios { get; set; } = new List<NotificacionUsuario>();


}

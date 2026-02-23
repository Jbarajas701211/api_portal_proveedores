
using System;

namespace ApiProveedores.Dto.Http;

public class LoginResponse
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTime RefreshExpiresAt { get; set; }
    public string Nombre { get; set; }
    public string Rol { get; set; }
}

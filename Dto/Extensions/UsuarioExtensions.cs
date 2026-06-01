using ApiProveedores.Dto.Entrada;
using ApiProveedores.Models;

namespace ApiProveedores.Dto.Extensions
{
    public static class UsuarioExtensions
    {
        public static UsuarioDto ToDto(this Usuario user)
        {
            return new UsuarioDto
            {
                Id = user.IdUsuario,
                Email = user.CorreoElectronico,
                Nombre = user.Nombre,
                ApellidoPaterno = user.ApellidoPaterno,
                ApellidoMaterno = user.ApellidoMaterno,
                Rol = "Anonimo"
            };
        }
    }

}

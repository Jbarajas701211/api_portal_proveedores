using System.Collections.Generic;

namespace ApiProveedores.Models
{
    public class Rol
    {
        public long IdRol { get; set; }
        public string Descripcion { get; set; }

        public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    }
}
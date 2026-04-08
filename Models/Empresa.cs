using System.Collections.Generic;

namespace ApiProveedores.Models
{
    public class Empresa
    {
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; }
        public string Rfc { get; set; }
        public bool Estatus { get; set; }
        public string Unidad { get; set; }

        public ICollection<UsuarioEmpresa> UsuarioEmpresas { get; set; } = new List<UsuarioEmpresa>();

        public ICollection<ProveedorEmpresa> ProveedorEmpresa { get; set; } = new List<ProveedorEmpresa>();
    }
}

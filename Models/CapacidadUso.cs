using ApiProveedores.Services;
using System;

namespace ApiProveedores.Models
{
    public class CapacidadUso
    {
        public long Id { get; set; }
        public string Cd { get; set; } = null!;
        public string Origen { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public int CantidadAsignada { get; set; }
        public TipoCapacidad Tipo { get; set; } = TipoCapacidad.ASIGNACION;
        public DateTime RegistradoEn { get; set; }
        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
    }

}

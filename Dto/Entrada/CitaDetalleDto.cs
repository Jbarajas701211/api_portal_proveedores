using System;

namespace ApiProveedores.Dto.Entrada
{

    public class BaseCitaDetalleDto
    {
        public long CitaId { get; set; }
        public string Oc { get; set; } = null!;
        public int CantidadPorCita { get; set; }
    }
    public class CrearCitaDetalleDto : BaseCitaDetalleDto
    {
    }

    public class ActualizarCitaDetalleDto : BaseCitaDetalleDto
    {
    }

    public class EliminaCitaDetalleDto : BaseCitaDetalleDto
    {
    }
}

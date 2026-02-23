using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;
using System.Collections.Generic;
using System.Linq;

namespace ApiProveedores.Dto.Mappers
{
    public static class CitaDetalleMapper
    {
        public static CitaDetalleDto ToDto(this CitaDetalle entity)
        {
            return new CitaDetalleDto
            {
                CitaId = entity.CitaId,
                Oc = entity.Oc,
                Origen = entity.Origen,
                CantidadPorCita = entity.CantidadPorCita,
                RegistradoEn = entity.RegistradoEn,
                CantidadTotal = entity.CantidadTotal,
                FechaVencimiento = entity.FechaVencimiento,
            };
        }
    }

    public static class CitaDetalleMapperList
    {
        public static List<CitaDetalleDto> ToDtoList(this IEnumerable<CitaDetalle> entities)
            => entities.Select(e => e.ToDto()).ToList();
    }
}

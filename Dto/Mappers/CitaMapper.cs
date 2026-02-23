using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;

namespace ApiProveedores.Dto.Mappers
{
    public static class CitaMapper
    {
        public static CitaDto ToDto(this Cita c) => new()
        {
            Id = c.Id,
            Lote = c.Lote,
            Folio = c.Folio,
            FechaCita = c.FechaCita,
            Cd = c.Cd,
            ProveedorId = c.ProveedorId,
            NombreSolicitante = c.NombreSolicitante,
            HoraCita = c.HoraCita,
            FechaSolicitud = c.FechaSolicitud,
            Estado = c.Estado,
            NombreChofer = c.NombreChofer,
            NombreAyudante = c.NombreAyudante,
            TipoUnidad = c.TipoUnidad,
            Placas = c.Placas,
            LineaTransportista = c.LineaTransportista,
            Observaciones = c.Observaciones,
            CreadoEn = c.CreadoEn,
            SolicitaAutorizacion = c.MarcadaParaSolicitarAutorizacion,
            Uuid = c.PublicId.ToString(),
        };
    }

}

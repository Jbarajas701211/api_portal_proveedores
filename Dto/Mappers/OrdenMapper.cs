using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;

namespace ApiProveedores.Dto.Mappers
{
    public static class OrdenMapper
    {
        public static OrdenDto ToDto(this Orden source)
        {
            if (source == null) return null;

            return new OrdenDto
            {
                Nopedido = source.Nopedido,
                Fechapedido = source.Fechapedido.ToString("dd/MM/yyyy"),
                Fechavenci = source.Fechavenci.ToString("dd/MM/yyyy"),
                Origen = source.Origen,
                Cveprov = source.Cveprov,
                Cd = source.Cd,
                CdDesc = source.CdDesc,
                Importe = source.Importe,
                Comprador = source.Comprador,
                Cantitotal = source.Cantitotal,
                Unnego = source.Unnego,
                Cvecateg = source.Cvecateg,
                Notacanc = source.Notacanc,
                Asn = source.Asn,
                Basico = source.Basico,
                TipoOrden = source.TipoOrden,
                CitaInd = source.CitaInd,
                OFlag = source.OFlag,
                OExMsg = source.OExMsg,
                ODttm = source.ODttm?.ToString("dd/MM/yyyy HH:mm"),
                ClaveAlmacen = source.CveAlmacen,

                Estatus = source.Notacanc == 1 ? "CANCELADA" : source.Status == 1 ? "ENTREGADA" : "INCOMPLETA",
                CantidadEntregada = source.CantidadEntregada,
                CantidadFaltante = source.CantidadFaltante,
                CantidadTeorica = source.CantidadTeorica,
                NombreCentroDistribucion = source?.CentroDistribucion?.Nombre ?? string.Empty,
            };
        }
    }

}

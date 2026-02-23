using System;

namespace ApiProveedores.Helper
{
    public static class CitaHelper
    {
        public static string GenerarFolioLote(long proveedorId, string numeroLote)
        {
            string fechaParte = DateTime.UtcNow.ToString("yyyyMMdd");
            string proveedorParte = proveedorId.ToString("D6");
            string numeroLoteParte = int.Parse(numeroLote).ToString("D2");
            return fechaParte + proveedorParte + numeroLoteParte;
        }
    }

}

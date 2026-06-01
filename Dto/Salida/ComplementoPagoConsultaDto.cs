using System;

namespace ApiProveedores.Dto.Salida
{
    public class ComplementoPagoConsultaDto
    {
        public int IdComplementoPago { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UuidPago { get; set; } = string.Empty;

        public DateTime FechaPago { get; set; }
        public string FormaPago { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public decimal TipoCambioPago { get; set; }
        public decimal MontoPago { get; set; }

        public string UuidFactura { get; set; } = string.Empty;
        public string SerieDocumentoRelacionado { get; set; } = string.Empty;
        public string FolioDocumentoRelacionado { get; set; } = string.Empty;

        public string? MetodoPago { get; set; }
        public int NumeroParcialidad { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;

        public string RfcProveedor { get; set; } = string.Empty;
        public long IdProveedor { get; set; }
        public string NumeroOperacion { get; set; }
    }
}


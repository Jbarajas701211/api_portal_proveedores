using System;

namespace ApiProveedores.Dto.Entrada
{
    public class CitasFiltroDto
    {
        public string? NumeroOrden { get; set; }
        public long? ProveedorId { get; set; }
        public long? IdCita { get; set; }
        public string? NumeroLote { get; set; }
        public string? Folio { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool SoloHoy { get; set; }
        public TipoConsultaCita Tipo { get; set; } = TipoConsultaCita.Todas;
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }
}

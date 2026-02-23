using System;
using System.Collections.Generic;

namespace ApiProveedores.Dto.Salida
{
    public class IncidenciaItemDto
    {
        public long Id { get; set; }
        public long CitaId { get; set; }
        public List<string> ClavesDescripcion { get; set; } = new();
        public string? Observacion { get; set; }
        public string? RutaArchivo { get; set; }
        public DateTime? RegistradoEn { get; set; }
    }
}

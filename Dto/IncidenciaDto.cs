using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiProveedores.Dto
{
    public class IncidenciaDto
    {
        public long CitaId { get; set; }
        public List<int> ClavesInc { get; set; } = new();
        public string? Observacion { get; set; }
    }


    public class IncidenciaMasivaDto
    {
        public long[] CitasId { get; set; }
        public int[] ClaveInc { get; set; }
        public string? Observacion { get; set; }
    }

    public class SolicitaUrlSessionEvidenciasDto
    {
        public int CitaId { get; set; }
        public int IncidenciaId { get; set; }
    }

    public class SolicitaUrlSessionEvidenciasMasivaDto
    {
        public long[] CitasId { get; set; }
        public string HashMasiva { get; set; }
    }

    public class SolicitaUrlSessionEvidenciasMasivoDto
    {
        public int[] CitasId { get; set; }
        public int IncidenciaId { get; set; }
    }
    public class MarcaArchivoCargadoDto: SolicitaUrlSessionEvidenciasDto
    {
        public bool Cargado { get; set; }
    }

    public class UrlDescargaEvidenciaDto : SolicitaUrlSessionEvidenciasDto
    {
    }
}

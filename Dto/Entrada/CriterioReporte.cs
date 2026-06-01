using ApiProveedores.Models.Enum;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApiProveedores.Dto.Entrada
{
    public class CriterioReporte
    {
        [JsonPropertyName("rfc")]
        [Required(ErrorMessage = "El campo RFC es obligatorio.")]
        public string Rfc { get; set; }
        [JsonPropertyName("reportType")]
        public TipoDocumento TipoDocumento { get; set; }
        [JsonPropertyName("documentTypes")]
        public TiposDocumentos TiposDocumentos { get; set; }
        [JsonPropertyName("startDate")]
        public DateTime FechaInicio { get; set; }
        [JsonPropertyName("endDate")]
        public DateTime FechaFinal { get; set; }
    }

    public class TiposDocumentos
    {
        [JsonPropertyName("ingreso")]
        public bool Ingreso { get; set; }
        [JsonPropertyName("egreso")]
        public bool Egreso { get; set; }
        [JsonPropertyName("complemento")]
        public bool Complemento { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace ApiProveedores.Dto.Salida
{
    public class ReporteGeneradoDto
    {
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}

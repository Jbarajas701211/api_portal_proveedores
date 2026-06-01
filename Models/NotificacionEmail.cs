using System.Text.Json.Serialization;

namespace ApiProveedores.Models
{
    public class NotificacionEmail
    {
        [JsonPropertyName("claveAplicativo")]
        public string ClaveAplicativo { get; set; } = string.Empty;
        [JsonPropertyName("nombreTemplate")]
        public string NombreTemplate { get; set; }
        [JsonPropertyName("emailDestino")]
        public string EmailDestino { get; set; }
        [JsonPropertyName("data")]
        public object Data { get; set; }
    }
}

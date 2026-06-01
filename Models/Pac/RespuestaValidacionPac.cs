using System.Text.Json.Serialization;

namespace ApiProveedores.Models.Pac
{
    public class RespuestaValidacionPac
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("detail")]
        public List<SeccionValidacion> Detail { get; set; }

        [JsonPropertyName("cadenaOriginalSAT")]
        public string CadenaOriginalSat { get; set; }

        [JsonPropertyName("cadenaOriginalComprobante")]
        public string CadenaOriginalComprobante { get; set; }

        [JsonPropertyName("uuid")]
        public Guid Uuid { get; set; }

        [JsonPropertyName("statusSat")]
        public string StatusSat { get; set; }

        [JsonPropertyName("statusCodeSat")]
        public string StatusCodeSat { get; set; }

        [JsonPropertyName("isCancelable")]
        public string IsCancelable { get; set; }

        [JsonPropertyName("statusCancelation")]
        public string StatusCancelation { get; set; }
    }

    public class SeccionValidacion
    {
        [JsonPropertyName("detail")]
        public List<DetalleMensaje> Detail { get; set; }

        [JsonPropertyName("section")]
        public string Section { get; set; }
    }

    public class DetalleMensaje
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("messageDetail")]
        public string MessageDetail { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("typeValue")]
        public string TypeValue { get; set; }
    }
}

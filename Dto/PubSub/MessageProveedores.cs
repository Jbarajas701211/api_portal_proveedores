using System.Text.Json.Serialization;

namespace ApiProveedores.Dto.PubSub
{
    public class MessageProveedores
    {
        [JsonPropertyName("to")]
        public string To { get; set; }
        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;
        [JsonPropertyName("app-name")]
        public string AppName { get; set; } = string.Empty;
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;
    }
}

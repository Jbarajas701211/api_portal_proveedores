using System;
using System.Text.Json.Serialization;

namespace ApiProveedores.Dto.PubSub
{
    public class MessageCuentaBloqueada : MessageCuentaBase
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

}

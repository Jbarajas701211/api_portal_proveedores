using System;
using System.Text.Json.Serialization;

namespace ApiProveedores.Dto.PubSub
{
    public class MessageCuentaBase
    {
        [JsonPropertyName("nombre")]
        public string NombreCompleto { get; set; }
        [JsonPropertyName("to")]
        public string Email { get; set; }
    }

}

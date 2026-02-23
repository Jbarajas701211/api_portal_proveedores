using System;
using System.Text.Json.Serialization;

namespace ApiProveedores.Dto.PubSub
{
    public class MessageWrapper<T>
    {
        [JsonPropertyName("payload")]
        public T Payload { get; set; }

        [JsonPropertyName("type-notification")]
        public string TypeNotification { get; set; }
    }

}

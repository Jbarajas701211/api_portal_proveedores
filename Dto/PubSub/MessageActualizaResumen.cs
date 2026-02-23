using System;
using System.Text.Json.Serialization;

namespace ApiProveedores.Dto.PubSub
{
    public class MessageActualizaResumen
    {
        [JsonPropertyName("cd")]
        public string Cd { get; set; }

        [JsonPropertyName("cita_id")]
        public long CitaId { get; set; }

        [JsonPropertyName("estado_cita")]
        public string EstadoCita { get; set; }

        [JsonPropertyName("fecha")]
        public DateTime Fecha { get; set; }

        [JsonPropertyName("proveedor_id")]
        public long ProveedorId { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace ApiProveedores.Models.Pac
{
    public class RespuestaAutenticacion
    {
        [JsonPropertyName("data")]
        public TokenData Data { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class TokenData
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonPropertyName("tokeny_type")] // Conserva el typo que viene del endpoint (tokeny_type)
        public string TokenType { get; set; }
    }
}

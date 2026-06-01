using System.Text.Json.Serialization;

namespace ApiProveedores.Models.Pac
{
    public class CredencialesPac
    {
        [JsonPropertyName("user")]
        public string User { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }
}

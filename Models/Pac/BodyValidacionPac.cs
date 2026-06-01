using System.Text.Json.Serialization;

namespace ApiProveedores.Models.Pac
{
    public class BodyValidacionPac
    {
        [JsonPropertyName("xml")]
        public IFormFile Xml { get; set; }
    }
}

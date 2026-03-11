using System.Text.Json.Serialization;

namespace ApiProveedores.Dto.Entrada
{
    public class ProveedorDocumentoDto
    {
        [JsonPropertyName("id_proveedor")]
        public long IdProveedor { get; set; }
        [JsonPropertyName("id_documento")]
        public int DocumentoId { get; set; }
        [JsonPropertyName("opcional")]
        public bool Opcional { get; set; }
    }
}
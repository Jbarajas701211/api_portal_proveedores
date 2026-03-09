using System.ComponentModel.DataAnnotations.Schema;

namespace ApiProveedores.Models
{
    public class ProveedorDocumento
    {
        public int Id { get; set; }

        // Cambiado a long para que coincida con Proveedor.Id_proveedor
        public long IdProveedor { get; set; }

        public int DocumentoId { get; set; }

        public bool Opcional { get; set; }

        [ForeignKey(nameof(IdProveedor))]
        public Proveedor? Proveedor { get; set; }

        [ForeignKey(nameof(DocumentoId))]
        public Documento? Documento { get; set; }
    }
}

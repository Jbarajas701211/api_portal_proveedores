using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApiProveedores.Models
{
    public class Proveedor
    {
        public long Id_proveedor { get; set; }
        public string Nombre { get; set; }
        public string Rfc { get; set; }
        public int VendorId { get; set; }
        public bool Estatus { get; set; }
        public int Sobrante { get; set; }
        public int PorcentajeSobrante { get; set; }
        public bool AplicarTolerancia { get; set; }
        public int IdCategoria { get; set; }
        public bool AcreedorSinXml { get; set; }
        public bool AplicarToleranciaCategoria { get; set; }
        public string EmailProveedor { get; set; }
        public bool DocFiscal { get; set; }
        public bool Factura { get; set; }
        public bool Recepcion { get; set; }
        public string Origen { get; set; }
        public string RazonSocial { get; set; }
        public string EntityId { get; set; }

        public ICollection<ProveedorEmpresa> ProveedorEmpresa { get; set; } = new List<ProveedorEmpresa>();

    }

}

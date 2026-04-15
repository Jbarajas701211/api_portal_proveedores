using ApiProveedores.Controllers;

namespace ApiProveedores.Dto.Proveedor
{
    public class ProveedorResponseDto
    {
        public long IdProveedor { get; set; }
        public string? Nombre { get; set; }
        public string? Rfc { get; set; }
        public string? IdVendor { get; set; }
        public bool Estatus { get; set; }
        public decimal Sobrante { get; set; }
        public decimal PorcentajeSobrante { get; set; }
        public decimal Faltante { get; set; }
        public decimal FaltantePorcentaje { get; set; }
        public bool AplicarTolerancia { get; set; }
        public  List<EmpresaDto>? Empresas { get; set; }

    }
}

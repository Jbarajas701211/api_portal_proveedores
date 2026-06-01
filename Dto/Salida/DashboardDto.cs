using ApiProveedores.Dto.Entrada;
using ApiProveedores.Models.Factura;

namespace ApiProveedores.Dto.Salida
{
    public class DashboardDto
    {
        public int FacturasProcesadas { get; set; }
        public int OrdenesPendientes { get; set; }
        public int ProcesosCompletados { get; set; }
        public List<Factura>? ListaFacturas { get; set; }
    }
}

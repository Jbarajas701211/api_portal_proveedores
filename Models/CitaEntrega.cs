using System;

namespace ApiProveedores.Models;

public enum EntregaEstatus
{
    ENTREGO = 1,
    FALLO = 2
}

public class CitaEntrega
{
    public long CitaId { get; set; }
    public DateOnly FechaEntrega { get; set; }
    public TimeOnly HoraRecepcion { get; set; }
    public int CantidadEntregada { get; set; }
    public EntregaEstatus Estatus { get; set; }
    public string? Anden { get; set; }
    public string? Acuse { get; set; }
    public string? Notas { get; set; }
    public DateTime RegistradoEn { get; set; }
}

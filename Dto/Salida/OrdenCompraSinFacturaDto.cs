using System;
using System.Collections.Generic;

namespace ApiProveedores.Dto.Salida;

/// <summary>
/// Orden de compra con recepciones sin referencias circulares (adecuado para JSON).
/// </summary>
public class OrdenCompraSinFacturaDto
{
    public long IdOrdenCompra { get; set; }
    public string ErpOrigen { get; set; } = null!;
    public string IdExterno { get; set; } = null!;
    public string? Folio { get; set; }
    public DateTime? FechaOc { get; set; }
    public string? Moneda { get; set; }
    public decimal? Total { get; set; }
    public string? ProveedorId { get; set; }
    public string? ProveedorNombre { get; set; }
    public string? ProveedorRfc { get; set; }
    public string? Sociedad { get; set; }
    public string? Subsidiaria { get; set; }
    public List<RecepcionSinFacturaItemDto> Recepciones { get; set; } = new();
}

/// <summary>
/// Recepción embebida en la respuesta (sin navegación de vuelta a la orden, para evitar ciclo en JSON).
/// </summary>
public class RecepcionSinFacturaItemDto
{
    public long IdRecepcion { get; set; }
    public long IdOrdenCompra { get; set; }
    public string ErpOrigen { get; set; } = null!;
    public string IdExterno { get; set; } = null!;
    public string? Folio { get; set; }
    public DateTime? FechaRecepcion { get; set; }
    public DateTime? FechaContabilizacion { get; set; }
    public string? Moneda { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? Total { get; set; }
    public string? Estado { get; set; }
    public decimal Cantidad { get; set; }
}

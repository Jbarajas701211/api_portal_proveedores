using System;
using System.Collections.Generic;
using System.Globalization;

namespace ApiProveedores.Models.Factura;

/// <summary>
/// Vista agregada de un CFDI ya deserializado, lista para reglas de negocio y validaciones.
/// </summary>
public class FacturaCfdiDocumento
{
    public CfdiComprobante Comprobante { get; set; } = null!;

    public TimbreFiscalDigital? TimbreFiscalDigital =>
        Comprobante.Complemento?.TimbreFiscalDigital;

    public string? Uuid => TimbreFiscalDigital?.Uuid;

    public string? RfcEmisor => Comprobante.Emisor?.Rfc;

    public string? NombreEmisor => Comprobante.Emisor?.Nombre;

    public string? RfcReceptor => Comprobante.Receptor?.Rfc;

    public string? NombreReceptor => Comprobante.Receptor?.Nombre;

    public string? UsoCfdiReceptor => Comprobante.Receptor?.UsoCfdi;

    public IReadOnlyList<CfdiConcepto> Conceptos =>
        Comprobante.Conceptos?.Concepto ?? (IReadOnlyList<CfdiConcepto>)Array.Empty<CfdiConcepto>();

    public decimal? SubTotal => ParseDecimal(Comprobante.SubTotal);

    public decimal? Total => ParseDecimal(Comprobante.Total);

    public decimal? TotalImpuestosTrasladados =>
        ParseDecimal(Comprobante.Impuestos?.TotalImpuestosTrasladados);

    public string? Moneda => Comprobante.Moneda;

    public string? TipoDeComprobante => Comprobante.TipoDeComprobante;

    public string? MetodoPago => Comprobante.MetodoPago;

    public string? FormaPago => Comprobante.FormaPago;

    public DateTime? FechaComprobante => ParseFechaIso(Comprobante.Fecha);

    public DateTime? FechaTimbrado => ParseFechaIso(TimbreFiscalDigital?.FechaTimbrado);

    public static FacturaCfdiDocumento From(CfdiComprobante comprobante)
    {
        ArgumentNullException.ThrowIfNull(comprobante);
        return new FacturaCfdiDocumento { Comprobante = comprobante };
    }

    private static decimal? ParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static DateTime? ParseFechaIso(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal | DateTimeStyles.RoundtripKind, out var dt)
            ? dt
            : null;
    }
}

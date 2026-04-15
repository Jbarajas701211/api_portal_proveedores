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

    public DateTime? FechaComprobante
    {
        get
        {
            // El Comprobante.Fecha puede venir en varios formatos ISO; usar ParseFechaIso para tolerancia
            if (Comprobante == null || string.IsNullOrWhiteSpace(Comprobante.Fecha))
                return null;

            return ParseFechaIso(Comprobante.Fecha);
        }
    }

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

    // Se corrige el uso de DateTimeStyles: RoundtripKind no puede mezclarse con AssumeLocal/AssumeUniversal/AdjustToUniversal
    private static DateTime? ParseFechaIso(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;

        // Intentar formatos comunes ISO 8601 con y sin offset
        var formats = new[]
        {
                "yyyy-MM-ddTHH:mm:ss.fffK",
                "yyyy-MM-ddTHH:mm:ssK",
                "yyyy-MM-ddTHH:mm:ss.fff",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-dd"
            };

        // Primero intentar ParseExact con estilos que no mezclen RoundtripKind con Assume*
        if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dtExact))
        {
            return dtExact;
        }

        // Si falla, intentar Parse con DateTimeStyles.AdjustToUniversal o None según contenido
        // Si la cadena contiene zona (Z o +/-) usar AssumeUniversal/AdjustToUniversal
        var styles = DateTimeStyles.None;
        if (s.EndsWith("Z", StringComparison.OrdinalIgnoreCase) || s.Contains("+") || s.Contains("-"))
        {
            styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;
        }

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, styles, out var dt))
        {
            return dt;
        }

        // Fallback: intentar parse sin provider
        if (DateTime.TryParse(s, out var dtFallback))
            return dtFallback;

        return null;
    }
}

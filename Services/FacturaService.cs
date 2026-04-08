using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ApiProveedores.Models.Factura;
using ApiProveedores.Services.Exceptions;

namespace ApiProveedores.Services;

/// <summary>
/// Lectura y materialización de CFDI (XML) a modelos tipados para validaciones posteriores.
/// </summary>
public class FacturaService
{
    private static readonly XmlSerializer Serializer = new(typeof(CfdiComprobante));

    /// <summary>
    /// Deserializa un CFDI 4.0 desde un stream (posición inicial; no cierra el stream).
    /// </summary>
    public FacturaCfdiDocumento ObtenerFacturaDesdeXml(Stream xmlStream)
    {
        if (xmlStream == null)
            throw new ArgumentNullException(nameof(xmlStream));

        try
        {
            if (xmlStream.CanSeek)
                xmlStream.Position = 0;

            var settings = new XmlReaderSettings
            {
                CloseInput = false,
                IgnoreWhitespace = true,
                DtdProcessing = DtdProcessing.Prohibit
            };

            using var reader = XmlReader.Create(xmlStream, settings);
            var obj = Serializer.Deserialize(reader);
            if (obj is not CfdiComprobante comprobante)
                throw new ApiProveedoresException("El XML no corresponde a un Comprobante CFDI válido.");

            return FacturaCfdiDocumento.From(comprobante);
        }
        catch (ApiProveedoresException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            throw new ApiProveedoresException($"No se pudo leer el XML de la factura: {ex.Message}");
        }
        catch (XmlException ex)
        {
            throw new ApiProveedoresException($"XML inválido: {ex.Message}");
        }
    }

    /// <summary>
    /// Sobrecarga para contenido en memoria (UTF-8).
    /// </summary>
    public FacturaCfdiDocumento ObtenerFacturaDesdeXml(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ApiProveedoresException("El contenido XML está vacío.");

        var bytes = Encoding.UTF8.GetBytes(xmlContent);
        using var ms = new MemoryStream(bytes, writable: false);
        return ObtenerFacturaDesdeXml(ms);
    }
}

using System.Collections.Generic;
using System.Xml.Serialization;

namespace ApiProveedores.Models.Factura;

[XmlRoot("Comprobante", Namespace = CfdiNamespaces.Cfdi40)]
public class CfdiComprobante
{
    [XmlElement("Emisor", Namespace = CfdiNamespaces.Cfdi40)]
    public CfdiEmisor? Emisor { get; set; }

    [XmlElement("Receptor", Namespace = CfdiNamespaces.Cfdi40)]
    public CfdiReceptor? Receptor { get; set; }

    [XmlElement("Conceptos", Namespace = CfdiNamespaces.Cfdi40)]
    public CfdiConceptos? Conceptos { get; set; }

    [XmlElement("Impuestos", Namespace = CfdiNamespaces.Cfdi40)]
    public CfdiImpuestosComprobante? Impuestos { get; set; }

    [XmlElement("Complemento", Namespace = CfdiNamespaces.Cfdi40)]
    public CfdiComplemento? Complemento { get; set; }

    [XmlAttribute("Version")]
    public string? Version { get; set; }

    [XmlAttribute("Serie")]
    public string? Serie { get; set; }

    [XmlAttribute("Folio")]
    public string? Folio { get; set; }

    [XmlAttribute("Fecha")]
    public string? Fecha { get; set; }

    [XmlAttribute("Sello")]
    public string? Sello { get; set; }

    [XmlAttribute("FormaPago")]
    public string? FormaPago { get; set; }

    [XmlAttribute("NoCertificado")]
    public string? NoCertificado { get; set; }

    [XmlAttribute("Certificado")]
    public string? Certificado { get; set; }

    [XmlAttribute("CondicionesDePago")]
    public string? CondicionesDePago { get; set; }

    [XmlAttribute("SubTotal")]
    public string? SubTotal { get; set; }

    [XmlAttribute("Moneda")]
    public string? Moneda { get; set; }

    [XmlAttribute("TipoCambio")]
    public string? TipoCambio { get; set; }

    [XmlAttribute("Total")]
    public string? Total { get; set; }

    [XmlAttribute("TipoDeComprobante")]
    public string? TipoDeComprobante { get; set; }

    [XmlAttribute("Exportacion")]
    public string? Exportacion { get; set; }

    [XmlAttribute("MetodoPago")]
    public string? MetodoPago { get; set; }

    [XmlAttribute("LugarExpedicion")]
    public string? LugarExpedicion { get; set; }
}

public class CfdiEmisor
{
    [XmlAttribute("Nombre")]
    public string? Nombre { get; set; }

    [XmlAttribute("Rfc")]
    public string? Rfc { get; set; }

    [XmlAttribute("RegimenFiscal")]
    public string? RegimenFiscal { get; set; }
}

public class CfdiReceptor
{
    [XmlAttribute("UsoCFDI")]
    public string? UsoCfdi { get; set; }

    [XmlAttribute("RegimenFiscalReceptor")]
    public string? RegimenFiscalReceptor { get; set; }

    [XmlAttribute("DomicilioFiscalReceptor")]
    public string? DomicilioFiscalReceptor { get; set; }

    [XmlAttribute("Nombre")]
    public string? Nombre { get; set; }

    [XmlAttribute("Rfc")]
    public string? Rfc { get; set; }
}

public class CfdiConceptos
{
    [XmlElement("Concepto", Namespace = CfdiNamespaces.Cfdi40)]
    public List<CfdiConcepto> Concepto { get; set; } = new();
}

public class CfdiConcepto
{
    [XmlElement("Impuestos", Namespace = CfdiNamespaces.Cfdi40)]
    public CfdiImpuestosConcepto? Impuestos { get; set; }

    [XmlAttribute("ClaveProdServ")]
    public string? ClaveProdServ { get; set; }

    [XmlAttribute("NoIdentificacion")]
    public string? NoIdentificacion { get; set; }

    [XmlAttribute("Cantidad")]
    public string? Cantidad { get; set; }

    [XmlAttribute("ClaveUnidad")]
    public string? ClaveUnidad { get; set; }

    [XmlAttribute("Unidad")]
    public string? Unidad { get; set; }

    [XmlAttribute("Descripcion")]
    public string? Descripcion { get; set; }

    [XmlAttribute("ValorUnitario")]
    public string? ValorUnitario { get; set; }

    [XmlAttribute("Importe")]
    public string? Importe { get; set; }

    [XmlAttribute("ObjetoImp")]
    public string? ObjetoImp { get; set; }
}

public class CfdiImpuestosConcepto
{
    [XmlElement("Traslados", Namespace = CfdiNamespaces.Cfdi40)]
    public CfdiTrasladosConcepto? Traslados { get; set; }
}

public class CfdiTrasladosConcepto
{
    [XmlElement("Traslado", Namespace = CfdiNamespaces.Cfdi40)]
    public List<CfdiTrasladoDetalle> Traslado { get; set; } = new();
}

public class CfdiImpuestosComprobante
{
    [XmlElement("Traslados", Namespace = CfdiNamespaces.Cfdi40)]
    public CfdiTrasladosComprobante? Traslados { get; set; }

    [XmlAttribute("TotalImpuestosTrasladados")]
    public string? TotalImpuestosTrasladados { get; set; }
}

public class CfdiTrasladosComprobante
{
    [XmlElement("Traslado", Namespace = CfdiNamespaces.Cfdi40)]
    public List<CfdiTrasladoResumen> Traslado { get; set; } = new();
}

public class CfdiTrasladoDetalle
{
    [XmlAttribute("Base")]
    public string? Base { get; set; }

    [XmlAttribute("Importe")]
    public string? Importe { get; set; }

    [XmlAttribute("Impuesto")]
    public string? Impuesto { get; set; }

    [XmlAttribute("TipoFactor")]
    public string? TipoFactor { get; set; }

    [XmlAttribute("TasaOCuota")]
    public string? TasaOCuota { get; set; }
}

public class CfdiTrasladoResumen
{
    [XmlAttribute("Base")]
    public string? Base { get; set; }

    [XmlAttribute("Importe")]
    public string? Importe { get; set; }

    [XmlAttribute("Impuesto")]
    public string? Impuesto { get; set; }

    [XmlAttribute("TipoFactor")]
    public string? TipoFactor { get; set; }

    [XmlAttribute("TasaOCuota")]
    public string? TasaOCuota { get; set; }
}

public class CfdiComplemento
{
    [XmlElement("TimbreFiscalDigital", Namespace = CfdiNamespaces.TimbreFiscalDigital)]
    public TimbreFiscalDigital? TimbreFiscalDigital { get; set; }
}

[XmlRoot("TimbreFiscalDigital", Namespace = CfdiNamespaces.TimbreFiscalDigital)]
public class TimbreFiscalDigital
{
    [XmlAttribute("Version")]
    public string? Version { get; set; }

    [XmlAttribute("UUID")]
    public string? Uuid { get; set; }

    [XmlAttribute("FechaTimbrado")]
    public string? FechaTimbrado { get; set; }

    [XmlAttribute("RfcProvCertif")]
    public string? RfcProvCertif { get; set; }

    [XmlAttribute("SelloCFD")]
    public string? SelloCfd { get; set; }

    [XmlAttribute("NoCertificadoSAT")]
    public string? NoCertificadoSat { get; set; }

    [XmlAttribute("SelloSAT")]
    public string? SelloSat { get; set; }
}

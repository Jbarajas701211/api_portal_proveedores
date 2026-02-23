using ApiProveedores.Services.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ApiProveedores.Services.Helper
{
    public static class HelperFechasReportes
    {
        public static DateTime? TryGetDate(IDictionary<string, object> filtros, string key)
        {
            if (!filtros.TryGetValue(key, out var raw) || raw is null) return null;

            if (raw is DateTime dt) return dt;

            var s = raw.ToString();
            if (string.IsNullOrWhiteSpace(s)) return null;

            if (DateTime.TryParse(s, new CultureInfo("es-MX"), DateTimeStyles.AssumeLocal, out var parsed)) return parsed;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed)) return parsed;

            throw new ReporteException($"El valor de '{key}' no tiene un formato de fecha válido.");
        }
    }
}

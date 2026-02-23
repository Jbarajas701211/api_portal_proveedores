using System;
using System.Globalization;


namespace ApiProveedores.Services.Helper
{
    public class HelperFechaHora
    {
        private const string DefaultTz = "America/Mexico_City";

        private static readonly string[] HoraFormats = new[]
        {
            @"H\:mm", @"HH\:mm", @"H\:mm\:ss", @"HH\:mm\:ss"
        };

        public static DateTime ToUtcTimestamp(string hora, DateTime fechaLocal, string? timeZoneId = null)
        {
            if (string.IsNullOrWhiteSpace(hora))
                throw new ArgumentException("La hora es requerida.", nameof(hora));

            if (!TimeSpan.TryParseExact(hora.Trim(), HoraFormats, CultureInfo.InvariantCulture, out var h))
                throw new ArgumentException("Formato de hora inválido. Usa HH:mm u HH:mm:ss.", nameof(hora));

            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId ?? DefaultTz);

            var fechaUnspecified = DateTime.SpecifyKind(fechaLocal.Date, DateTimeKind.Unspecified);
            var fechaHoraLocal = fechaUnspecified.Add(h);

            if (tz.IsInvalidTime(fechaHoraLocal))
                throw new ArgumentOutOfRangeException(nameof(hora), "La hora especificada no existe en esa fecha por cambio de horario.");

            var offset = tz.GetUtcOffset(fechaHoraLocal);

            // Construimos un DateTimeOffset con el offset correcto y devolvemos en UTC
            var dto = new DateTimeOffset(fechaHoraLocal, offset);
            return dto.UtcDateTime;
        }

        public static bool TryToUtcTimestamp(string hora, DateTime fechaLocal, out DateTime utc, string? timeZoneId = null)
        {
            utc = default;
            if (string.IsNullOrWhiteSpace(hora)) return false;
            if (!TimeSpan.TryParseExact(hora.Trim(), HoraFormats, CultureInfo.InvariantCulture, out var h)) return false;

            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId ?? DefaultTz);
            var fechaUnspecified = DateTime.SpecifyKind(fechaLocal.Date, DateTimeKind.Unspecified);
            var fechaHoraLocal = fechaUnspecified.Add(h);
            if (tz.IsInvalidTime(fechaHoraLocal)) return false;

            var offset = tz.GetUtcOffset(fechaHoraLocal);
            utc = new DateTimeOffset(fechaHoraLocal, offset).UtcDateTime;
            return true;
        }
    }
}

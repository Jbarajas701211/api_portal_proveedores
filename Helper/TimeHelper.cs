using System;

namespace ApiProveedores.Helper
{
    public class TimeHelper
    {

        public static DateTime UtcNow()
        {
            return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        }

        public static DateTime NowMexicoUnspecified()
        {
            var tzMx = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzMx);
        }
    }
}

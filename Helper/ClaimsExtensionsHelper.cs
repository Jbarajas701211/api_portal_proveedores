
using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace ApiProveedores.Helper
{
    public static class ClaimsExtensionsHelper
    {

        private static readonly string[] UserIdClaimTypes =
        {
            "sub",
            ClaimTypes.NameIdentifier,
            "uid", "user_id"
        };

        private static readonly string[] ProveedorIdClaimTypes =
        {
            "proveedorId",
            ClaimTypes.GroupSid,
            "gid"
        };

        public static bool TryGetUserId(this ClaimsPrincipal user, out long userId)
            => TryGetLongFromClaims(user, UserIdClaimTypes, out userId);

        public static bool TryGetProveedorId(this ClaimsPrincipal user, out long proveedorId)
            => TryGetLongFromClaims(user, ProveedorIdClaimTypes, out proveedorId);

        public static (long userId, long proveedorId) RequireIds(this ClaimsPrincipal user)
        {
            if (!user.TryGetUserId(out var uid))
                throw new InvalidOperationException("El token no contiene un userId válido.");

            if (!user.TryGetProveedorId(out var pid)) {}

            return (uid, pid);
        }

        private static bool TryGetLongFromClaims(ClaimsPrincipal user, string[] claimTypes, out long value)
        {
            value = 0;
            var claim = claimTypes
                .Select(t => user.FindFirst(t)?.Value)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            if (claim is null) return false;

            // Invariante para evitar problemas de cultura
            return long.TryParse(claim, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }
}

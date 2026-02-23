using System;

namespace ApiProveedores.Helper
{
    public static class PasswordHasher
    {
        public static string Hashear(string password, int workFactor = 12)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
        }

        public static bool Verificar(string passwordPlano, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(passwordPlano, hash);
        }
    }
}

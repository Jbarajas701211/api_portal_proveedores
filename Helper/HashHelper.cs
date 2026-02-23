using System;
using System.Security.Cryptography;
using System.Text;

namespace ApiProveedores.Helper
{
    public static class HashHelper
    {

        public static string Sha256(string texto)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(texto);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        public static string RandomHash(int bytesLength = 32)
        {
            var buffer = new byte[bytesLength];
            RandomNumberGenerator.Fill(buffer);
            return Convert.ToHexString(buffer);
        }
    }
}

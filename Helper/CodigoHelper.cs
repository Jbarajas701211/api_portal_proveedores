namespace ApiProveedores.Helper
{
    using System;
    using System.Linq;

    public static class CodigoHelper
    {
        private static readonly char[] _caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        private static readonly Random _random = new Random();

        public static string GenerarCodigoAlfanumerico(int longitud = 10)
        {
            return new string(Enumerable.Range(0, longitud)
                .Select(_ => _caracteres[_random.Next(_caracteres.Length)])
                .ToArray());
        }
    }

}

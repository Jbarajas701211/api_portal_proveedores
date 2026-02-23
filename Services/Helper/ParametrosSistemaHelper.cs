using System.Text.RegularExpressions;

namespace ApiProveedores.Services.Helper
{
    public static class ParametrosSistemaHelper
    {
        private static readonly Regex regexClaveValida = new Regex("^[A-Z_]+$");

        public static bool EsClaveValida(string clave)
        {
            return !string.IsNullOrWhiteSpace(clave) && regexClaveValida.IsMatch(clave);
        }
    }
}

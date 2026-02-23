using System;

namespace ApiProveedores.Services.Exceptions
{
    public class ActivacionCuentaException : ApiProveedoresException
    {
        private const string DefaultMessage = "Link de activaci�n inv�lido.";
        public ActivacionCuentaException() : base() { }

        public ActivacionCuentaException(string message) : base(message) { }

        public ActivacionCuentaException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

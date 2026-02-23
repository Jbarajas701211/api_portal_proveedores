using System;

namespace ApiProveedores.Services.Exceptions
{
    public class RecuperacionCuentaException : ApiProveedoresException
    {
        private const string DefaultMessage = "Link de recuperaci�n inv�lido.";
        public RecuperacionCuentaException() : base(DefaultMessage) { }

        public RecuperacionCuentaException(string message) : base(message) { }

        public RecuperacionCuentaException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

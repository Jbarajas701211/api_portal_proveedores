using System;

namespace ApiProveedores.Services.Exceptions
{
    public class FirmaCuentaException : ApiProveedoresException
    {
        private const string DefaultMessage = "Firma inv�lida.";
        public FirmaCuentaException() : base(DefaultMessage) { }

        public FirmaCuentaException(string message) : base(message) { }

        public FirmaCuentaException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

using System;

namespace ApiProveedores.Services.Exceptions
{
    public class AltaCuentaException : ApiProveedoresException
    {
        private const string DefaultMessage = "El email ya est� registrado.";

        public AltaCuentaException() : base(DefaultMessage) { }

        public AltaCuentaException(string message) : base(message) { }

        public AltaCuentaException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

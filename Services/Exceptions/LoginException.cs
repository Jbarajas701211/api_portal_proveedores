using System;

namespace ApiProveedores.Services.Exceptions
{
    public class LoginException : ApiProveedoresException
    {
        private const string DefaultMessage = "Link de activaci�n inv�lido.";
        public LoginException() : base(DefaultMessage) { }

        public LoginException(string message) : base(message) { }

        public LoginException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

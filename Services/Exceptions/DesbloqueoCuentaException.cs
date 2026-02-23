using System;

namespace ApiProveedores.Services.Exceptions
{
    public class DesbloqueoCuentaException : ApiProveedoresException
    {
        public DesbloqueoCuentaException() : base() { }

        public DesbloqueoCuentaException(string message) : base(message) { }

        public DesbloqueoCuentaException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

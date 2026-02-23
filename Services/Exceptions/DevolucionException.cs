using System;

namespace ApiProveedores.Services.Exceptions
{
    public class DevolucionException : ApiProveedoresException
    {
        public DevolucionException() : base() { }

        public DevolucionException(string message) : base(message) { }

        public DevolucionException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

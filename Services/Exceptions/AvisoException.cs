using System;

namespace ApiProveedores.Services.Exceptions
{
    public class AvisoException : ApiProveedoresException
    {
        public AvisoException() : base() { }

        public AvisoException(string message) : base(message) { }

        public AvisoException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

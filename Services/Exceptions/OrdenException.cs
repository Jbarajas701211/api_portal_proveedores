using System;

namespace ApiProveedores.Services.Exceptions
{
    public class OrdenException : ApiProveedoresException
    {
        public OrdenException() : base() { }

        public OrdenException(string message) : base(message) { }

        public OrdenException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

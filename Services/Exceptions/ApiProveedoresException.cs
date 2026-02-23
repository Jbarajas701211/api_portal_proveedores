using System;

namespace ApiProveedores.Services.Exceptions
{
    public class ApiProveedoresException : Exception
    {
        public ApiProveedoresException() : base() { }

        public ApiProveedoresException(string message) : base(message) { }

        public ApiProveedoresException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

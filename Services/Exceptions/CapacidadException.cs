using System;

namespace ApiProveedores.Services.Exceptions
{
    public class CapacidadException : ApiProveedoresException
    {
        public CapacidadException() : base() { }

        public CapacidadException(string message) : base(message) { }

        public CapacidadException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

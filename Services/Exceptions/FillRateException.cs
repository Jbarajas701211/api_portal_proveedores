using System;

namespace ApiProveedores.Services.Exceptions
{
    public class KpiProveedorException : ApiProveedoresException
    {
        public KpiProveedorException() : base() { }

        public KpiProveedorException(string message) : base(message) { }

        public KpiProveedorException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

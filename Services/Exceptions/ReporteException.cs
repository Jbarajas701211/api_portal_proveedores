using System;

namespace ApiProveedores.Services.Exceptions
{
    public class ReporteException : ApiProveedoresException
    {
        public ReporteException() : base() { }

        public ReporteException(string message) : base(message) { }

        public ReporteException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

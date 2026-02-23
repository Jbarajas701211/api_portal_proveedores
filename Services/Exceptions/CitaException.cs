using System;

namespace ApiProveedores.Services.Exceptions
{
    public class CitaException : ApiProveedoresException
    {
        public CitaException() : base() { }

        public CitaException(string message) : base(message) { }

        public CitaException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    public class DetalleCitaException : ApiProveedoresException
    {
        public DetalleCitaException() : base() { }

        public DetalleCitaException(string message) : base(message) { }

        public DetalleCitaException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

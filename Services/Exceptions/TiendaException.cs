using System;

namespace ApiProveedores.Services.Exceptions
{
    public class TiendaException : ApiProveedoresException
    {
        public TiendaException() : base() { }

        public TiendaException(string message) : base(message) { }

        public TiendaException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

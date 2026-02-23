using System;

namespace ApiProveedores.Services.Exceptions
{
    public class DiaNoLaborableException : ApiProveedoresException
    {
        public DiaNoLaborableException() : base() { }

        public DiaNoLaborableException(string message) : base(message) { }

        public DiaNoLaborableException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

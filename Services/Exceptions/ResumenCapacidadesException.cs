using System;

namespace ApiProveedores.Services.Exceptions
{
    public class ResumenCapacidadesException : ApiProveedoresException
    {
        public ResumenCapacidadesException() : base() { }

        public ResumenCapacidadesException(string message) : base(message) { }

        public ResumenCapacidadesException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

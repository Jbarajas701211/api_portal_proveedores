using System;

namespace ApiProveedores.Services.Exceptions
{
    public class ParametroSistemaException : ApiProveedoresException
    {
        public ParametroSistemaException() : base() { }

        public ParametroSistemaException(string message) : base(message) { }

        public ParametroSistemaException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

using System;

namespace ApiProveedores.Services.Exceptions
{
    public class NotificationException : ApiProveedoresException
    {
        public NotificationException() : base() { }

        public NotificationException(string message) : base(message) { }

        public NotificationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

using ApiProveedores.Services.PubSub;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ApiProveedores.Services.Reportes
{
    public abstract class BasePubSubService
    {
        public GenericPubSubPublisher _publisher { get; }

        protected BasePubSubService(GenericPubSubPublisher publisher)
        {
            _publisher = publisher;
        }

        protected async Task EnviarMensajeAsync(string topic, object mensaje)
        {
            await _publisher.PublicarAsync(topic, mensaje);
        }

        public abstract Task GenerarReporteAsync(IDictionary<string, object> filtros, ClaimsPrincipal user);
    }

}

using System.Threading.Tasks;
using ApiProveedores.Services.PubSub;
using System;
using ApiProveedores.Dto.PubSub;
using ApiProveedores.Models;
using ApiProveedores.Helper;


namespace ApiProveedores.Services
{

    public enum EstadoCita
    {
        CREADA,
        ELIMINADA,
        AGENDADA,
        ENTREGADA,
        FALLO,
        CANCELADA,
        REAGENDADA,
        SOLICITA_AUTORIZACION,
        AUTORIZADA,
        DENEGADA
    }

    public class ActualizaResumenService
    {

        private readonly PublisherResumenService _pubSubPublisher;

        public ActualizaResumenService(PublisherResumenService pubSubPublisher)
        {
            _pubSubPublisher = pubSubPublisher;
        }


        public async Task ActualizaResumen(Cita cita, EstadoCita estado)
        {
            MessageActualizaResumen message = new MessageActualizaResumen() { 
                Cd = cita.Cd,
                Fecha = TimeHelper.NowMexicoUnspecified(),
                CitaId = cita.Id,
                EstadoCita = estado.ToString().ToLower(),
                ProveedorId = cita.ProveedorId
            };

            await _pubSubPublisher.EnviarNotificacionAsync(message);
        }

       
    }
}

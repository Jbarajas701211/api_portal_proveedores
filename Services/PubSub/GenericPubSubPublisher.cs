using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using Google.Api.Gax;

namespace ApiProveedores.Services.PubSub
{
    public class GenericPubSubPublisher
    {
        private readonly string _projectId;

        public GenericPubSubPublisher()
        {
            var projectIdFromEnv = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
            _projectId = projectIdFromEnv ?? Platform.Instance().ProjectId ?? throw new Exception("Project ID no encontrado");
        }

        public async Task PublicarAsync(string topicId, object mensaje)
        {
            if (string.IsNullOrWhiteSpace(topicId))
                throw new ArgumentNullException(nameof(topicId));

            var publisher = await PublisherClient.CreateAsync(
                TopicName.FromProjectTopic(_projectId, topicId)
            );

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(mensaje, options);

            var pubsubMessage = new PubsubMessage
            {
                Data = ByteString.CopyFromUtf8(json)
            };

            await publisher.PublishAsync(pubsubMessage);
        }
    }
}

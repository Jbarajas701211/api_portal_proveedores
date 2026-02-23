namespace ApiProveedores.Services.PubSub
{
    using Google.Api.Gax;
    using Google.Cloud.PubSub.V1;
    using Google.Protobuf;
    using System.Text.Json;
    using System;

    using System.Threading.Tasks;

    public class PublisherPnjService
    {
        private readonly string _projectId;
        private readonly string _topicId;
        public PublisherPnjService(string topicId)
        {

            var projectIdFromEnv = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
            if (projectIdFromEnv != null)
            {
                _projectId = projectIdFromEnv;
            }
            else {
                _projectId = Platform.Instance().ProjectId ?? throw new Exception("Project ID no encontrado");
            }
            
            _topicId = topicId ?? throw new ArgumentNullException(nameof(topicId));
        }

        public async Task EnviarNotificacionAsync(object mensaje)
        {
            PublisherClient publisher = await PublisherClient.CreateAsync(
                TopicName.FromProjectTopic(_projectId, _topicId)
            );

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(mensaje, options);

            var message = new PubsubMessage
            {
                Data = ByteString.CopyFromUtf8(json)
            };

            await publisher.PublishAsync(message);
        }
    }

}

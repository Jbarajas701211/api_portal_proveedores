namespace Marti.PortalCitas.Onest.Services.Core
{
    using ApiProveedores.Interfaces;
    using ApiProveedores.Models.Pac;
    using Grpc.Core;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Logging;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;


    public class ServiceHttp: IServiceHttp
    {
        private readonly ILogger<ServiceHttp> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ClientName = "ApiClientGenerico";

        public ServiceHttp(ILogger<ServiceHttp> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<RespuestaAutenticacion> AutenticarAsync<TBody>(string urlCompleta, TBody credenciales)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{urlCompleta}/v2/security/authenticate")
            {
                Content = JsonContent.Create(credenciales)
            };
            return await SendRequestAsync<RespuestaAutenticacion>(request, token: null);
        }

        public async Task<TResult> GetAsync<TResult>(string endpoint, string? token = null, Dictionary<string, string?>? queryParams = null)
        {
            if (queryParams != null) endpoint = QueryHelpers.AddQueryString(endpoint, queryParams);
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            return await SendRequestAsync<TResult>(request, token); 
        }

        public async Task<TResult> PostAsync<TResult, TBody>(string endpoint, TBody body, string? token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(body) };
            return await SendRequestAsync<TResult>(request, token); 
        }

        public async Task<TResult> PostMultipartFileAsync<TResult>(
            string endpoint,
            byte[] fileContent,
            string formFieldName,
            string fileName,
            string contentType,
            string? token = null)
        {
            using var multipart = new MultipartFormDataContent();
            var fileStreamContent = new ByteArrayContent(fileContent);
            fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            multipart.Add(fileStreamContent, formFieldName, fileName);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = multipart };
            return await SendRequestAsync<TResult>(request, token);
        }

        public async Task<TResult> PutAsync<TResult, TBody>(string endpoint, TBody body, string? token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, endpoint) { Content = JsonContent.Create(body) };
            return await SendRequestAsync<TResult>(request, token); 
        }

        public async Task<TResult> PatchAsync<TResult, TBody>(string endpoint, TBody body, string? token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) { Content = JsonContent.Create(body) };
            return await SendRequestAsync<TResult>(request, token); 
        }


        private async Task<TResult> SendRequestAsync<TResult>(HttpRequestMessage request, string? token)
        {
            // A) Crea el cliente gestionado por la factoría para evitar Socket Exhaustion
            var client = _httpClientFactory.CreateClient(ClientName);

            // B) Si el método requería autenticación, aquí añade automáticamente el "Bearer token"
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // C) Ejecuta físicamente la llamada HTTP a través de internet
            using var response = await client.SendAsync(request);

            // D) Si el PAC o el API responde un error (400, 401, 500), lee el error y detiene el flujo
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error en API: {(int)response.StatusCode} {response.ReasonPhrase}. Detalle: {errorContent}");
            }

            // E) Si todo salió bien, toma el JSON y lo transforma mágicamente en tu clase de C#
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = await response.Content.ReadFromJsonAsync<TResult>(options);

            if (result == null)
            {
                throw new InvalidOperationException("La respuesta de la API no pudo ser deserializada al tipo especificado.");
            }

            return result;
        }
    }

}

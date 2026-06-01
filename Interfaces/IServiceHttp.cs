using ApiProveedores.Models.Pac;

namespace ApiProveedores.Interfaces
{
    public interface IServiceHttp
    {
        Task<RespuestaAutenticacion> AutenticarAsync<TBody>(string urlCompleta, TBody credenciales);
        Task<TResult> GetAsync<TResult>(string endpoint, string? token = null, Dictionary<string, string?>? queryParams = null);
        Task<TResult> PostAsync<TResult, TBody>(string endpoint, TBody body, string? token = null);
        Task<TResult> PostMultipartFileAsync<TResult>(
            string endpoint,
            byte[] fileContent,
            string formFieldName,
            string fileName,
            string contentType,
            string? token = null);
        Task<TResult> PutAsync<TResult, TBody>(string endpoint, TBody body, string? token = null);
        Task<TResult> PatchAsync<TResult, TBody>(string endpoint, TBody body, string? token = null);
       
    }
}

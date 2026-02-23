using System.Net;

namespace ApiProveedores.Dto.Salida
{
    public class ApiResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public object? Data { get; set; }
    }
}

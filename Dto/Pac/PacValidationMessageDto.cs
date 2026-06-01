using ApiProveedores.Dto.Pac.Enum;

namespace ApiProveedores.Dto.Pac
{
    public class PacValidationMessageDto
    {
        public string Message { get; set; } = string.Empty;

        public string? Detail { get; set; }

        public PacValidationMessageType Type
        {
            get; set;
        }
    }
}

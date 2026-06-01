namespace ApiProveedores.Dto.Pac
{
    public class PacValidationSectionDto
    {
        public string Section { get; set; } = string.Empty;

        public List<PacValidationMessageDto> Messages { get; set; } = [];
    }
}

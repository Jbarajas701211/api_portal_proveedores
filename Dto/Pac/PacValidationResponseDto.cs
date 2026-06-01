namespace ApiProveedores.Dto.Pac
{
    public class PacValidationResponseDto
    {
        public bool IsSuccess { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Uuid { get; set; }

        public string? StatusSat { get; set; }

        public string? StatusCodeSat { get; set; }

        public string? IsCancelable { get; set; }

        public string? StatusCancellation { get; set; }

        public string? CadenaOriginalSat { get; set; }

        public string? CadenaOriginalComprobante { get; set; }

        public List<PacValidationSectionDto> Sections { get; set; } = [];
    }
}

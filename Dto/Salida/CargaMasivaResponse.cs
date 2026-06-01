namespace ApiProveedores.Dto.Salida
{
    public class CargaMasivaResponse
    {
        public List<ResultadoCargaFacturaGrupalDto> Procesados { get; set; } = new List<ResultadoCargaFacturaGrupalDto>();
        public List<ResultadoCargaFacturaGrupalDto> NoProcesados { get; set; } = new List<ResultadoCargaFacturaGrupalDto>();
    }
}

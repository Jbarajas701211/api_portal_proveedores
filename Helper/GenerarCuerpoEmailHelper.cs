using ApiProveedores.Dto.Salida;
using System.Text;

namespace ApiProveedores.Helper
{
    public class GenerarCuerpoEmailHelper
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public GenerarCuerpoEmailHelper(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;
        }

        public async Task<string> GeneraBodyResultadoCargaFacturaMasiva(CargaMasivaResponse resultadoCargaFacturaGrupalDtos, string nombre)
        {
            var nombreArchivo = _config[nombre];
            var layout = await ObtenerTemplateAsync(nombreArchivo ?? throw new ArgumentNullException(nameof(nombreArchivo)));

            if(layout == null)
                throw new ArgumentNullException(nameof(layout));

            StringBuilder filasHtml = new StringBuilder();

            foreach (var resultado in resultadoCargaFacturaGrupalDtos.Procesados)
            {
                string colorMensaje = "#28a745"; // Verde para procesados
                string iconoProcesada = "\"✅ SI\"";

                filasHtml.Append("<tr>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd;'>{resultado.OrdenCompra}</td>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd;'>{resultado.Recepcion}</td>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd;'>{resultado.Identificador}</td>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd;'>{resultado.Factura}</td>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd; color: {colorMensaje}'>{resultado.Mensaje}</td>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd; text-align: center;'>{iconoProcesada}</td>");
                filasHtml.Append("</tr>");
            }

            foreach (var resultado in resultadoCargaFacturaGrupalDtos.NoProcesados)
            {
                string colorMensaje = "#dc3545"; // Rojo para no procesados
                string iconoProcesada = "\"❌ NO\"";
                filasHtml.Append("<tr>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd;'>{resultado.OrdenCompra}</td>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd;'>{resultado.Recepcion}</td>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd;'>{resultado.Identificador}</td>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd;'>{resultado.Factura}</td>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd; color: {colorMensaje}'>{resultado.Mensaje}</td>");
                filasHtml.Append($"<td style='border: 1px solid #dddddd; text-align: center;'>{iconoProcesada}</td>");
                filasHtml.Append("</tr>");
            }

            return layout.Replace("{{FILAS_TABLA}}", filasHtml.ToString());
        }


        public async Task<string> ObtenerTemplateAsync(string nombreArchivo)
        {
            var ruta = Path.Combine(
                _env.ContentRootPath,
                "EmailTemplate",
                nombreArchivo);

            if(!File.Exists(ruta))
                throw new FileNotFoundException("El archivo de plantilla no fue encontrado.");

            var contenido = await File.ReadAllTextAsync(ruta);

            return contenido;
        }
    }
}

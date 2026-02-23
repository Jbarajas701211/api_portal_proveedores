using System.Collections.Generic;

namespace ApiProveedores.Dto.Paginadores
{
    public class ResultadoPaginado<T>
    {
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalElementos { get; set; }
        public List<T> Elementos { get; set; } = new();
    }

}

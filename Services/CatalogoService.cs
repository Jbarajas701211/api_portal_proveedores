using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Models;
using ApiProveedores.Services.Exceptions;
using Google.Api;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ApiProveedores.Dto.Catalogos;

namespace ApiProveedores.Services
{
    public class CatalogoService
    {
        private readonly PortalDbContext _context;

        public CatalogoService(PortalDbContext context)
        {
            _context = context;
        }

        public async Task<List<DocumentoTipoDto>> RecuperaTipoDocumento()
        {
            try
            {
                var documentos = await _context.Documento
                                    .Select(d => new DocumentoTipoDto
                                    {
                                        Descripcion = d.Descripcion,
                                        IdDocumento = d.Id,
                                        Tipo = d.Tipo
                                    }).ToListAsync();

                return documentos;
            }
            catch (Exception)
            {

                throw new ApiProveedoresException("No se obtuvieron los documentos");
            }
            
        }
       

    }
}

using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Models;
using ApiProveedores.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ApiProveedores.Dto;

namespace ApiProveedores.Services
{
    public class OrdenCompraService
    {
        private readonly PortalDbContext _context;

        public OrdenCompraService(PortalDbContext context)
        {
            _context = context;
        }
      

        public async Task<ResultadoPaginado<ProveedorDto>> BuscarProveedoresPaginadoAsync(string? filtro, int pagina, int tamanioPagina)
        {
            if (pagina <= 0) pagina = 1;
            if (tamanioPagina <= 0) tamanioPagina = 10;

            var query = _context.Proveedores.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var filtroNorm = filtro.Trim();

                query = query.Where(p =>
                    EF.Functions.ILike(p.Nombre, $"%{filtroNorm}%"));
            }

            var total = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling((double)total / tamanioPagina);

            var proveedores = await query
                .OrderBy(p => p.Nombre)
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .Select(p => new ProveedorDto
                {
                    Id = p.Id_proveedor,
                    NombreProveedor = p.Nombre,
                    ClaveProveedor = p.VendorId.ToString(),
                    Estatus = p.Estatus,
                    Rfc = p.Rfc,
                    Sobrante = p.Sobrante,
                    PorcentajeSobrante = p.PorcentajeSobrante,
                    Faltante = p.Faltante,
                    PorcentajeFaltante = p.PorcentajeFaltante,
                    AplicarTolerancia = p.AplicarTolerancia,
                    IdCategoria = p.IdCategoria,
                    AccredorSinXml = p.AcreedorSinXml,
                    AplicarToleranciaCategoria = p.AplicarToleranciaCategoria,
                    Email = p.EmailProveedor,
                    DocumentoFiscal = p.DocFiscal,
                    Factura = p.Factura,
                    Recepcion = p.Recepcion,
                    Origen = p.Origen,
                    RazonSocial = p.RazonSocial

                })
                .ToListAsync();

            return new ResultadoPaginado<ProveedorDto>
            {
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalElementos = total,
                Elementos = proveedores
            };
        }
   


        public async Task<bool> ExisteRfcAsync(string rfc)
        {
            if (string.IsNullOrWhiteSpace(rfc))
                throw new ApiProveedoresException("RFC invùlido.");

            try
            {
                var rfcNorm = rfc.Replace(" ", "").ToUpper();

                return await _context.Proveedores
                    .AnyAsync(p => p.Rfc != null && p.Rfc.Replace(" ", "").ToUpper() == rfcNorm);
            }
            catch (Exception)
            {
                throw new ApiProveedoresException("Error al validar el RFC.");
            }
        }


        public async Task<List<RecepcionResponseDto>> ObtenerRecepcionesPorIdOcAsync(string idExterno)
        {
            if (string.IsNullOrWhiteSpace(idExterno))
                throw new ApiProveedoresException("Orden de compra invùlido.");

            try
            {
                var existe = await _context.OrdenesCompras.AnyAsync(x => x.IdExterno == idExterno);

                if(!existe)
                    throw new ApiProveedoresException("No se encontrù la orden de compra.");

                var result = await _context.Recepciones
                    .Where(r => r.OrdenCompra.IdExterno == idExterno)
                    .Select(r => new RecepcionResponseDto
                    {
                        IdRecepcion = r.IdRecepcion,
                        Fecha = r.FechaRecepcion,
                        Cantidad = r.Detalles.Sum(d => d.Cantidad ?? 0),
                        Monto = r.Total
                    }).ToListAsync();


                return result;
            }
            catch (Exception ex)
            {
                throw new ApiProveedoresException(ex.Message ?? "Error al obtener informaciùn del RFC.");
            }
        }

        public async Task<bool> ValidaSiCuentaConOrdenesCompraSinFactura(string idProveedor)
        {
            if (string.IsNullOrWhiteSpace(idProveedor))
                throw new ApiProveedoresException("No cuentas con ordenes de compra pendientes de facturar");

            try
            {
                return await _context.OrdenesCompras
                    .AnyAsync(o => o.ProveedorId == idProveedor
                        && !o.Recepciones.Any(r => r.FacturaRecepcion.Any()));
            }
            catch (Exception ex)
            {

                throw new ApiProveedoresException($"Error: {ex.Message} ");
            }
            
        }

        // Obtener ùrdenes de compra que no cuentan con factura (ninguna recepciùn con factura)
        public async Task<List<OrdenCompraSinFacturaDto>> GetOrdenesSinFacturaAsync(string idProveedor)
        {
            if (string.IsNullOrWhiteSpace(idProveedor))
                throw new ApiProveedoresException("El identificador de proveedor es obligatorio.");

            var ordenes = await _context.OrdenesCompras
                .AsNoTracking()
                .Include(o => o.Recepciones)
                .Where(o => o.ProveedorId == idProveedor
                    && !o.Recepciones.Any(r => r.FacturaRecepcion.Any()))
                .ToListAsync();

            return ordenes.Select(MapOrdenSinFactura).ToList();
        }

        private static OrdenCompraSinFacturaDto MapOrdenSinFactura(OrdenCompra o)
        {
            return new OrdenCompraSinFacturaDto
            {
                IdOrdenCompra = o.IdOrdenCompra,
                ErpOrigen = o.ErpOrigen,
                IdExterno = o.IdExterno,
                Folio = o.Folio,
                FechaOc = o.FechaOc,
                Moneda = o.Moneda,
                Total = o.Total,
                ProveedorId = o.ProveedorId,
                ProveedorNombre = o.ProveedorNombre,
                ProveedorRfc = o.ProveedorRfc,
                Sociedad = o.Sociedad,
                Subsidiaria = o.Subsidiaria,
                Recepciones = o.Recepciones
                    .Select(r => new RecepcionSinFacturaItemDto
                    {
                        IdRecepcion = r.IdRecepcion,
                        IdOrdenCompra = r.IdOrdenCompra,
                        ErpOrigen = r.ErpOrigen,
                        IdExterno = r.IdExterno,
                        Folio = r.Folio,
                        FechaRecepcion = r.FechaRecepcion,
                        FechaContabilizacion = r.FechaContabilizacion,
                        Moneda = r.Moneda,
                        Subtotal = r.Subtotal,
                        Total = r.Total,
                        Estado = r.Estado
                    })
                    .ToList()
            };
        }
    }
}

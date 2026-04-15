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


        public async Task<List<RecepcionResponseDto>> ObtenerRecepcionesPorIdOcAsync(string idExterno)
        {
            if (string.IsNullOrWhiteSpace(idExterno))
                throw new ApiProveedoresException("Orden de compra invùlido.");

            try
            {
                var existe = await _context.Recepciones.AnyAsync(x => x.IdRecepcion.ToString() == idExterno);

                if(!existe)
                    throw new ApiProveedoresException("No se encontrù la orden de compra.");

                var result = await _context.Recepciones
                    .Where(r => r.OrdenCompra.IdExterno == idExterno)
                    .Select(r => new RecepcionResponseDto
                    {
                        IdRecepcion = r.IdRecepcion,
                        Fecha = r.FechaRecepcion,
                        Cantidad = r.Detalles.Sum(d => d.Cantidad ?? 0),
                        Monto = r.Subtotal
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

        // Obtener ùrdenes de compra que no cuentan con factura (ninguna recepciùon con factura)
        public async Task<List<OrdenCompraSinFacturaDto>> GetOrdenesSinFacturaAsync(string rfcProveedor, string ordenCompra)
        {
            if (string.IsNullOrWhiteSpace(rfcProveedor) || string.IsNullOrWhiteSpace(ordenCompra))
                throw new ApiProveedoresException("La informaciÛn no se est· enviando completa.");

            var ordenes = await _context.OrdenesCompras
                .AsNoTracking()
                .Include(o => o.Recepciones)
                .Where(o => o.ProveedorRfc == rfcProveedor
                    && o.Folio == ordenCompra && !o.Recepciones.Any(r => r.FacturaRecepcion.Any()))
                .ToListAsync();

            return ordenes.Select(MapOrdenSinFactura).ToList();
        }

        public async Task<OrdenCompraSinFacturaDto> GetOrdenRecepcionSinFacturaAsync(string rfcProveedor, string ordenCompra)
        {
            if (string.IsNullOrWhiteSpace(rfcProveedor) || string.IsNullOrWhiteSpace(ordenCompra))
                throw new ApiProveedoresException("La informaciÛn no se est· enviando completa.");

            var ordenes = await _context.OrdenesCompras
                .AsNoTracking()
                .Include(o => o.Recepciones)
                .Where(o => o.ProveedorRfc == rfcProveedor
                    && o.Folio == ordenCompra && !o.Recepciones.Any(r => r.FacturaRecepcion.Any())).FirstOrDefaultAsync();

            return ordenes != null ? MapOrdenSinFactura(ordenes) : new OrdenCompraSinFacturaDto();
        }

        public async Task<OrdenCompraSinFacturaDto> GetOrdenIdRecepcionAsync(string rfcProveedor, string ordenCompra, long idRecepcion)
        {
            if (string.IsNullOrWhiteSpace(rfcProveedor) || string.IsNullOrWhiteSpace(ordenCompra) || idRecepcion == 0)
                throw new ApiProveedoresException("La informaciÛn no se est· enviando completa.");

            var orden = await _context.OrdenesCompras
                .AsNoTracking()
                .Include(o => o.Recepciones.Where(r => r.IdRecepcion == idRecepcion))
                .Where(o => o.ProveedorRfc == rfcProveedor
                    && o.Folio == ordenCompra).FirstOrDefaultAsync();

            return orden != null ? MapOrdenSinFactura(orden) : new OrdenCompraSinFacturaDto();
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
                        Estado = r.Estado,
                        Cantidad = r.Cantidad ?? 0
                    })
                    .ToList()
            };
        }
    }
}

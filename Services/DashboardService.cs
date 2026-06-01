using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Dto.Proveedor;
using ApiProveedores.Dto.PubSub;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Helper;
using ApiProveedores.Models;
using ApiProveedores.Models.Enum;
using ApiProveedores.Models.Factura;
using ApiProveedores.Services.Exceptions;
using ApiProveedores.Services.PubSub;
using ClosedXML.Excel;
using Google.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using FacturaEntidad = ApiProveedores.Models.Factura.Factura;

namespace ApiProveedores.Services;

/// <summary>
/// Lectura y materialización de CFDI (XML) a modelos tipados para validaciones posteriores.
/// </summary>
public class DashboardService
{
    private readonly OrdenCompraService _ordenCompraService;
    private readonly ProveedoresService _proveedoresService;
    private readonly PortalDbContext _db;
    private readonly ILogger<DashboardService> _logger;
    private readonly FacturaService _facturaService;

    public DashboardService(PortalDbContext db, ILogger<DashboardService> logger, FacturaService facturaService, OrdenCompraService ordenCompraService, ProveedoresService proveedoresService)
    {
        _db = db;
        _logger = logger;
        _facturaService = facturaService;
        _ordenCompraService = ordenCompraService;
        _proveedoresService = proveedoresService;
    }



    public async Task<ApiResponseDto<DashboardDto>> ObtenerInformacionDashboardAsync(int? idUsuario, DateTime? fechaInicial, DateTime? fechaFinal)
    {
        _logger.LogInformation("Iniciando proceso de obtención de información para el proveedor {IdUsuario}.", idUsuario);
       

        try
        {
            DashboardDto informacion = new DashboardDto();
            // se obtiene al usuario para obtener el RFC del proveedor y posteriormente obtener su id para hacer la consulta de facturas
            var usuario = await _db.Usuarios
                .Include(u => u.UsuarioRoles)
                .ThenInclude(ur => ur.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
            if (usuario == null)
                return new ApiResponseDto<DashboardDto>
                {
                    Message = $"No se encontró un usuario con el ID {idUsuario}.",
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                    Success = false,
                    Data = null
                };

            List<OrdenCompra>? listadoOrdenes = null;
            ResultadoPaginado<Factura> resultado = new ResultadoPaginado<Factura>();

            if (usuario.UsuarioRoles.FirstOrDefault()!.Rol.Descripcion != "PROVEEDOR")
            {
                listadoOrdenes = await _db.OrdenesCompras
                .AsNoTracking()
                .Include(o => o.Recepciones)
                .Where(o => !o.Recepciones.Any(r => r.FacturaRecepcion.Any()))
                .ToListAsync();

                resultado = await _facturaService.ConsultarFacturasAsync(0, 100, fechaInicial, fechaFinal, null);
            }
            else
            {
                var rfcProveedor = usuario.RfcProveedor;
                // se obtiene al proveedor para obtener id y hacer la validación de sobrante en caso de que la factura exceda el monto de la recepción
                var proveedor = await _proveedoresService.ObtenerInfoProveedorPorRfcAsync(usuario.RfcProveedor);
                var payloadProveedor = proveedor.Values.OfType<ProveedorResponseDto>().FirstOrDefault();

                if (payloadProveedor == null)
                {
                    _logger.LogError("No se encontró un proveedor con el RFC {RfcProveedor}.", usuario.RfcProveedor);
                    return new ApiResponseDto<DashboardDto>
                    {
                        Message = $"No se encontró un proveedor con el RFC {usuario.RfcProveedor}.",
                        StatusCode = System.Net.HttpStatusCode.NotFound,
                        Success = false,
                        Data = null
                    };

                }

                listadoOrdenes = await _db.OrdenesCompras
                    .AsNoTracking()
                    .Include(o => o.Recepciones)
                    .Where(o => o.ProveedorRfc == rfcProveedor
                         && !o.Recepciones.Any(r => r.FacturaRecepcion.Any()))
                    .ToListAsync();

                 resultado = await _facturaService.ConsultarFacturasAsync(0, 100, fechaInicial, fechaFinal, null, payloadProveedor.IdProveedor);
            }

            informacion.OrdenesPendientes = listadoOrdenes.Select(o => new OrdenCompraSinFacturaDto
            {
                Folio = o.Folio,
                Total = o.Total
            }).ToList().Count();

            informacion.FacturasProcesadas = resultado.Elementos.Where(x => x.FechaAlta >= fechaFinal?.AddDays(-7)).Count();
            informacion.ListaFacturas = resultado.Elementos;
            if(informacion.FacturasProcesadas == 0 || resultado.Elementos.Count() == 0)
            {
                return new ApiResponseDto<DashboardDto>
                {
                    Message = "Información del dashboard obtenida.",
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Success = true,
                    Data = informacion
                };
            }
            informacion.ProcesosCompletados = informacion.FacturasProcesadas / resultado.Elementos.Count(x => x.EstatusFactura == EstatusFacturaEnum.Procesada) * 100;

            return new ApiResponseDto<DashboardDto>
            {
                Message = "Información del dashboard obtenida.",
                StatusCode = System.Net.HttpStatusCode.OK,
                Success = true,
                Data = informacion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado en el proceso de obtener información del dashboard, error: {Message}", ex.Message);
            return new ApiResponseDto<DashboardDto>
            {
                Message = ex.Message,
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Success = false,
                Data = null
            };
        }
    }

   
}

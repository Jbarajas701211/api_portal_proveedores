using ApiProveedores.Dto;
using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Paginadores;
using ApiProveedores.Dto.Proveedor;
using ApiProveedores.Models;
using ApiProveedores.Services.Exceptions;
using Google.Api;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiProveedores.Services
{
    public class ProveedoresService
    {
        private readonly PortalDbContext _context;
        private readonly ILogger<ProveedoresService> _logger;
        private readonly OrdenCompraService _ordenCompraService;

        public ProveedoresService(PortalDbContext context, ILogger<ProveedoresService> logger, OrdenCompraService ordenCompraService)
        {
            _context = context;
            _logger = logger;
            _ordenCompraService = ordenCompraService;
        }

        public async Task<Proveedor> RecuperaProveedorAsync(long idProveedor)
        {
            var proveedor = await _context.Proveedores
              .FirstOrDefaultAsync(o => o.Id_proveedor == idProveedor);
            return proveedor;
        }

        public async Task<Proveedor> RecuperaProveedorPorIdAsync(int idProveedor)
        {
            var proveedor = await _context.Proveedores
              .FirstOrDefaultAsync(o => o.Id_proveedor == idProveedor);
            return proveedor;
        }

        public async Task<Proveedor> RecuperaProveedorPorEmail(string email)
        {
            var proveedor = await _context.Proveedores
              .FirstOrDefaultAsync(o => o.EmailProveedor == email);
            return proveedor;
        }

        public async Task<List<ApiProveedores.Dto.Salida.DocumentoProveedorDto>> ObtenerDocumentosPorProveedorAsync(long idProveedor)
        {
            return await _context.ProveedorDocumento
                .Include(pd => pd.Documento)
                .Where(pd => pd.IdProveedor == idProveedor)
                .Select(pd => new ApiProveedores.Dto.Salida.DocumentoProveedorDto
                {
                    Documento = pd.Documento.Descripcion,
                    Opcional = pd.Opcional
                })
                .ToListAsync();
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

        // Actualiza un proveedor buscando por Id exclusivamente.
        // Devuelve true si se guardó correctamente, false si no existe el registro.
        public async Task<bool> ActualizarProveedorAsync(ProveedorDto dto)
        {
            if (dto == null)
                throw new ApiProveedoresException("Datos de proveedor inválidos.");

            // Asegurarse de que venga un Id válido
            if (dto.Id <= 0)
            {
                return false;
            }

            try
            {
                var existente = await _context.Proveedores.FindAsync(dto.Id);

                if (existente == null)
                    return false;

                // Mapear campos del DTO a la entidad (solo campos esperados)
                existente.Nombre = dto.NombreProveedor ?? existente.Nombre;

                // Ajuste: VendorId es int en el modelo, parsear a int antes de asignar
                if (!string.IsNullOrWhiteSpace(dto.ClaveProveedor))
                {
                    existente.VendorId = dto.ClaveProveedor;
                }

                existente.Estatus = dto.Estatus;
                existente.Rfc = dto.Rfc;
                existente.Sobrante = dto.Sobrante;
                existente.PorcentajeSobrante = dto.PorcentajeSobrante;
                existente.Faltante = dto.Faltante;
                existente.PorcentajeFaltante = dto.PorcentajeFaltante;
                existente.AplicarTolerancia = dto.AplicarTolerancia;
                existente.IdCategoria = dto.IdCategoria == 0 ? 1 : dto.IdCategoria;
                existente.AcreedorSinXml = dto.AccredorSinXml;
                existente.AplicarToleranciaCategoria = dto.AplicarToleranciaCategoria;
                existente.EmailProveedor = dto.Email;
                existente.DocFiscal = dto.DocumentoFiscal;
                existente.Factura = dto.Factura;
                existente.Recepcion = dto.Recepcion;
                existente.Origen = dto.Origen;
                existente.RazonSocial = dto.RazonSocial;

                _context.Proveedores.Update(existente);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException)
            {
                throw new ApiProveedoresException("No se pudo actualizar el registro.");
            }
            catch (Exception)
            {
                throw new ApiProveedoresException("No se pudo actualizar el registro.");
            }
        }

        // Agregar una relación Proveedor - Documento
        public async Task<bool> AgregarDocumentoProveedorAsync(ProveedorDocumentoDto dto)
        {
            if (dto == null)
                throw new ApiProveedoresException("Datos inválidos.");

            var proveedor = await _context.Proveedores.FindAsync(dto.IdProveedor);
            if (proveedor == null)
                return false; // proveedor no existe

            var documento = await _context.Documento.FindAsync(dto.DocumentoId);
            if (documento == null)
                return false; // documento no existe

            var existe = await _context.ProveedorDocumento.AnyAsync(pd => pd.IdProveedor == dto.IdProveedor && pd.DocumentoId == dto.DocumentoId);
            if (existe)
                return false; // ya existe la relación

            var relacion = new ApiProveedores.Models.ProveedorDocumento
            {
                IdProveedor = dto.IdProveedor,
                DocumentoId = dto.DocumentoId,
                Opcional = dto.Opcional
            };

            try
            {
                _context.ProveedorDocumento.Add(relacion);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                throw new ApiProveedoresException("No se pudo agregar el documento al proveedor.");
            }
            catch (Exception)
            {
                throw new ApiProveedoresException("No se pudo agregar el documento al proveedor.");
            }
        }

        // Eliminar una relación Proveedor - Documento
        public async Task<bool> EliminarDocumentoProveedorAsync(long idProveedor, int documentoId)
        {
            var relacion = await _context.ProveedorDocumento
                .FirstOrDefaultAsync(pd => pd.IdProveedor == idProveedor && pd.DocumentoId == documentoId);

            if (relacion == null)
                return false; // no existe la relación

            try
            {
                _context.ProveedorDocumento.Remove(relacion);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                throw new ApiProveedoresException("No se pudo eliminar el documento del proveedor.");
            }
            catch (Exception)
            {
                throw new ApiProveedoresException("No se pudo eliminar el documento del proveedor.");
            }
        }


        // Actualizar una relación Proveedor - Documento
        public async Task<bool> ActualizarDocumentoProveedorAsync(ApiProveedores.Dto.Entrada.ProveedorDocumentoDto dto)
        {
            if (dto == null)
                throw new ApiProveedoresException("Datos inválidos.");

            var relacion = await _context.ProveedorDocumento
                .FirstOrDefaultAsync(pd => pd.IdProveedor == dto.IdProveedor && pd.DocumentoId == dto.DocumentoId);

            if (relacion == null)
                return false;

            relacion.Opcional = dto.Opcional;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                throw new ApiProveedoresException("No se pudo actualizar el documento del proveedor.");
            }
            catch (Exception)
            {
                throw new ApiProveedoresException("No se pudo actualizar el documento del proveedor.");
            }
        }

        // Agregar varias relaciones Proveedor - Documento en lote
        // Además elimina de la BD las relaciones existentes para cada proveedor que no se incluyan en la lista recibida.
        public async Task<bool> AgregarDocumentosProveedorAsync(List<ProveedorDocumentoDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                throw new ApiProveedoresException("Datos inválidos.");

            var proveedorIds = dtos.Select(d => d.IdProveedor).Distinct().ToList();
            var documentoIds = dtos.Select(d => d.DocumentoId).Distinct().ToList();

            // Validar existencia de proveedores y documentos referenciados
            var proveedoresExistentes = await _context.Proveedores
                .Where(p => proveedorIds.Contains(p.Id_proveedor))
                .Select(p => p.Id_proveedor)
                .ToListAsync();

            var documentosExistentes = await _context.Documento
                .Where(d => documentoIds.Contains(d.Id))
                .Select(d => d.Id)
                .ToListAsync();

            if (proveedoresExistentes.Count != proveedorIds.Count || documentosExistentes.Count != documentoIds.Count)
                return false; 

            await using var txn = await _context.Database.BeginTransactionAsync();
            try
            {
                // Para cada proveedor limpiar las relaciones que no vienen en la lista
                foreach (var pid in proveedorIds)
                {
                    var dtosParaProveedor = dtos.Where(d => d.IdProveedor == pid).Select(d => d.DocumentoId).ToHashSet();

                    var relacionesActuales = await _context.ProveedorDocumento
                        .Where(pd => pd.IdProveedor == pid)
                        .ToListAsync();

                    // Eliminar relaciones que existen en BD pero no en la lista entrante
                    var relacionesParaEliminar = relacionesActuales.Where(r => !dtosParaProveedor.Contains(r.DocumentoId)).ToList();
                    if (relacionesParaEliminar.Any())
                    {
                        _context.ProveedorDocumento.RemoveRange(relacionesParaEliminar);
                    }
                }

                // Cargar relaciones actuales restantes para evitar duplicados al insertar
                var relacionesExistentes = await _context.ProveedorDocumento
                    .Where(pd => proveedorIds.Contains(pd.IdProveedor))
                    .Select(pd => new { pd.IdProveedor, pd.DocumentoId })
                    .ToListAsync();

                // Agregar las relaciones que no existan aún
                foreach (var dto in dtos)
                {
                    var yaExiste = relacionesExistentes.Any(r => r.IdProveedor == dto.IdProveedor && r.DocumentoId == dto.DocumentoId);
                    if (yaExiste)
                        continue;

                    var relacion = new ProveedorDocumento
                    {
                        IdProveedor = dto.IdProveedor,
                        DocumentoId = dto.DocumentoId,
                        Opcional = dto.Opcional
                    };

                    _context.ProveedorDocumento.Add(relacion);
                }

                await _context.SaveChangesAsync();
                await txn.CommitAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                await txn.RollbackAsync();
                throw new ApiProveedoresException("No se pudo agregar los documentos al proveedor.");
            }
            catch (System.Exception)
            {
                await txn.RollbackAsync();
                throw new ApiProveedoresException("No se pudo agregar los documentos al proveedor.");
            }
        }

        // Actualizar varias relaciones Proveedor - Documento en lote
        // También elimina de la BD las relaciones existentes para cada proveedor que no se incluyan en la lista recibida.
        public async Task<bool> ActualizarDocumentosProveedorAsync(List<ProveedorDocumentoDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                throw new ApiProveedoresException("Datos inválidos.");

            var proveedorIds = dtos.Select(d => d.IdProveedor).Distinct().ToList();

            await using var txn = await _context.Database.BeginTransactionAsync();
            try
            {
                // Para cada proveedor, obtener relaciones actuales y comparar
                foreach (var pid in proveedorIds)
                {
                    var dtosParaProveedor = dtos.Where(d => d.IdProveedor == pid).ToList();
                    var documentoIdsParaDto = dtosParaProveedor.Select(d => d.DocumentoId).ToHashSet();

                    var relacionesActuales = await _context.ProveedorDocumento
                        .Where(pd => pd.IdProveedor == pid)
                        .ToListAsync();

                    // Eliminar relaciones en BD que no vienen en la lista
                    var relacionesParaEliminar = relacionesActuales.Where(r => !documentoIdsParaDto.Contains(r.DocumentoId)).ToList();
                    if (relacionesParaEliminar.Any())
                    {
                        _context.ProveedorDocumento.RemoveRange(relacionesParaEliminar);
                    }

                    // Actualizar o agregar las relaciones recibidas
                    foreach (var dto in dtosParaProveedor)
                    {
                        var rel = relacionesActuales.FirstOrDefault(r => r.DocumentoId == dto.DocumentoId);
                        if (rel != null)
                        {
                            rel.Opcional = dto.Opcional;
                        }
                        else
                        {
                            // Si no existe, crearla
                            var nueva = new ProveedorDocumento
                            {
                                IdProveedor = dto.IdProveedor,
                                DocumentoId = dto.DocumentoId,
                                Opcional = dto.Opcional
                            };
                            _context.ProveedorDocumento.Add(nueva);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await txn.CommitAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                await txn.RollbackAsync();
                throw new ApiProveedoresException("No se pudo actualizar los documentos del proveedor.");
            }
            catch (System.Exception)
            {
                await txn.RollbackAsync();
                throw new ApiProveedoresException("No se pudo actualizar los documentos del proveedor.");
            }
        }

        public async Task<bool> ExisteRfcAsync(string rfc)
        {
            if (string.IsNullOrWhiteSpace(rfc))
                throw new ApiProveedoresException("RFC inválido.");

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

        public async Task<Proveedor> ObtenerProveedorPorRfcAsync(string rfc)
        {
            if (string.IsNullOrWhiteSpace(rfc))
                throw new ApiProveedoresException("RFC inválido.");
            try
            {
                var rfcNorm = rfc.Replace(" ", "").ToUpper();
                var proveedor = await _context.Proveedores
                    .FirstOrDefaultAsync(p => p.Rfc != null && p.Rfc.Replace(" ", "").ToUpper() == rfcNorm);
                return proveedor;
            }
            catch (Exception ex)
            {
                throw new ApiProveedoresException(ex.Message ?? "Error al obtener información del RFC.");
            }
        }


        public async Task<Dictionary<string, object>> ObtenerInfoProveedorPorRfcAsync(string rfc)
        {
            if (string.IsNullOrWhiteSpace(rfc))
                throw new ApiProveedoresException("RFC inválido.");

            try
            {
                var rfcNorm = rfc.Replace(" ", "").ToUpperInvariant();

                var proveedor = await _context.Proveedores
                    .Include(p => p.ProveedorEmpresa)
                        .ThenInclude(pe => pe.Empresa)
                    .FirstOrDefaultAsync(p => p.Rfc != null && p.Rfc.Replace(" ", "").ToUpper() == rfcNorm);

                if (proveedor == null)
                {
                    var vacio = new
                    {
                        rfc = rfcNorm,
                        nombre = string.Empty,
                        registrado = false,
                        empresas = new List<object>()
                    };
                    return new Dictionary<string, object> { { rfcNorm, vacio } };
                }

                // Validación solicitada: si no trae empresas, lanzar excepción controlada
                if (proveedor.ProveedorEmpresa == null || !proveedor.ProveedorEmpresa.Any())
                    throw new ApiProveedoresException("Esta proveedor no tiene empresas asociadas");

                // Validar si cuenta con Ordenes de compra asociadas, si no tiene, lanzar excepción controlada
                var tieneOrdenesCompra = await _ordenCompraService.ValidaSiCuentaConOrdenesCompraSinFactura(proveedor.Id_proveedor.ToString());
                //if (!tieneOrdenesCompra)
                //throw new ApiProveedoresException("Esta proveedor no tiene ordenes de compra pendientes de factura");

                var nombreProveedor = string.IsNullOrWhiteSpace(proveedor.RazonSocial) ? proveedor.Nombre : proveedor.RazonSocial;

                var empresas = proveedor.ProveedorEmpresa?
                    .Select(pe => new EmpresaDto
                    {
                        IdEmpresa = pe.IdEmpresa,
                        Nombre = pe.Empresa?.Nombre ?? string.Empty
                    })
                    .ToList();



                var payload = new ProveedorResponseDto
                {
                    IdProveedor = proveedor.Id_proveedor,
                    Rfc = rfcNorm,
                    Nombre = nombreProveedor ?? string.Empty,
                    Estatus = proveedor.Estatus,
                    Empresas = empresas,
                    Sobrante = proveedor.Sobrante,
                    PorcentajeSobrante= proveedor.PorcentajeSobrante,
                    Faltante = proveedor.Faltante,
                    FaltantePorcentaje = proveedor.PorcentajeFaltante,
                    AplicarTolerancia = proveedor.AplicarTolerancia,
                    Email = proveedor.EmailProveedor
                };

                return new Dictionary<string, object> { { rfcNorm, payload } };
            }
            catch (Exception ex)
            {
                throw new ApiProveedoresException(ex.Message ?? "Error al obtener información del RFC.");
            }
        }
    }
}

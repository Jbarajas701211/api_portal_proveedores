using ApiProveedores.Dto;
using ApiProveedores.Dto.Entrada;
using ApiProveedores.Dto.Salida;
using ApiProveedores.Helper;
using ApiProveedores.Models;
using ApiProveedores.Services.Citas.Validators;
using ApiProveedores.Services.Exceptions;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ApiProveedores.Services.Citas
{
    public class IncidenciaService
    {
        private readonly PortalDbContext _context;
        private readonly IncidenciaValidator _validator;
        private readonly IncidenciaMasivaValidator _validatorIncMasiva;
        private readonly IncidenciaSolicitaUrlValidator _solicitaUrlValidator;

        public IncidenciaService(PortalDbContext context,
            IncidenciaValidator validator,
            IncidenciaMasivaValidator masivaValidator,
            IncidenciaSolicitaUrlValidator solicitaUrlValidator)
        {
            _context = context;
            _validator = validator;
            _validatorIncMasiva = masivaValidator;
            _solicitaUrlValidator = solicitaUrlValidator;
        }

        public async Task<IReadOnlyList<IncidenciaItemDto>> ObtenerIncidenciasPorCitaAsync(
            long citaId,
            CancellationToken ct = default)
        {
            return await _context.Set<CitaIncidencia>()
                .AsNoTracking()
                .Where(i => i.CitaId == citaId)
                .OrderByDescending(i => i.RegistradoEn)
                .Select(i => new IncidenciaItemDto
                {
                    Id = i.Id,
                    CitaId = i.CitaId,

                    ClavesDescripcion = i.Claves
                        .OrderBy(c => c.ClaveInc)
                        .Select(c => c.CatalogoIncidencia.Descripcion)
                        .ToList(),

                    Observacion = i.Observacion,
                    RegistradoEn = i.RegistradoEn,
                    RutaArchivo = i.RutaArchivo,
                })
                .ToListAsync(ct);
        }


        public async Task<IReadOnlyList<CatalogoIncidencia>> GetCatalogoIncidenciaAsync(CancellationToken ct = default)
        {

            return await _context.CatalogoIncidencias
                .AsNoTracking()
                .OrderBy(x => x.Clave)
                .ThenBy(x => x.Descripcion)
                .ToListAsync(ct);
        }

        public async Task<CatalogoIncidencia> CreateCatalogoIncidenciaAsync(IncidenciaRequestDto dto, CancellationToken ct = default)
        {
            var incidencia = await _context.CatalogoIncidencias
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Clave == dto.Clave, ct);

            if (incidencia is not null)
            {
                throw new CitaException($"Ya existe una incidencia con la clave {dto.Clave}.");
            }

            var newItem = new CatalogoIncidencia
            {
                Clave = dto.Clave,
                Descripcion = dto.Descripcion
            };
            try
            {
                _context.CatalogoIncidencias.Add(newItem);
                await _context.SaveChangesAsync(ct);
                return newItem;
            }
            catch (Exception ex)
            {
                throw new CitaException("Error al crear la incidencia en el catálogo.", ex);
            }
           
        }

        public async Task<CatalogoIncidencia> UpdateCatalogoIncidenciaAsync(IncidenciaRequestDto dto, CancellationToken ct = default)
        {
            var existingItem = await _context.CatalogoIncidencias
                .FirstOrDefaultAsync(x => x.Clave == dto.Clave, ct);
            if (existingItem == null)
            {
                throw new CitaException($"No se encontró la incidencia con clave {dto.Clave}.");
            }
            existingItem.Descripcion = dto.Descripcion;
            try
            {
                await _context.SaveChangesAsync(ct);
                return existingItem;
            }
            catch (Exception ex)
            {

                throw new CitaException("Error al actualizar la incidencia en el catálogo.", ex);
            }
            
        }


        public async Task<IncidenciaMasivaResultadoDto> RegistraIncidenciaMasivaAsync(
            IncidenciaMasivaDto dto,
            CancellationToken ct = default)
        {
            var ctx = new IncidenciaMasivaValidatorContext { Dto = dto };
            var result = await _validatorIncMasiva.ValidateAsync(ctx, ct);
            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            var incidencias = new List<CitaIncidencia>();
            var hashMasivo = HashHelper.RandomHash();

            var citasId = dto.CitasId ?? Array.Empty<long>();
            var clavesInc = dto.ClaveInc ?? Array.Empty<int>();

            foreach (var citaId in citasId)
            {
                if (clavesInc.Length == 0)
                    continue;

                var incidencia = new CitaIncidencia
                {
                    CitaId = citaId,
                    Observacion = dto.Observacion,
                    RegistradoEn = TimeHelper.NowMexicoUnspecified(),
                    HashMasivo = hashMasivo,
                    Claves = clavesInc
                        .Select(clave => new CitaIncidenciaClave
                        {
                            ClaveInc = clave
                        })
                        .ToList()
                };

                incidencias.Add(incidencia);
            }

            if (incidencias.Count == 0)
            {
                return new IncidenciaMasivaResultadoDto
                {
                    HashMasivo = hashMasivo,
                    IncidenciasIds = new List<long>()
                };
            }

            await _context.CitasIncidencias.AddRangeAsync(incidencias, ct);
            await _context.SaveChangesAsync(ct);

            var ids = incidencias.Select(i => i.Id).ToList();

            return new IncidenciaMasivaResultadoDto
            {
                HashMasivo = hashMasivo,
                IncidenciasIds = ids
            };
        }


        public async Task<CitaIncidencia> RegistraIncidenciaAsync(
            IncidenciaDto dto,
            CancellationToken ct = default)
        {
            // Secuencia de validaciones.
            var ctx = new IncidenciaValidatorContext
            {
                IdCita = dto.CitaId,
                Dto = dto
            };

            var result = await _validator.ValidateAsync(ctx, ct);
            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            var claves = dto.ClavesInc ?? new List<int>();

            var clavesDistinct = claves.Distinct().ToList();

            var incidencia = new CitaIncidencia
            {
                CitaId = dto.CitaId,
                RegistradoEn = TimeHelper.NowMexicoUnspecified(),
                Observacion = dto.Observacion,
                Claves = clavesDistinct
                    .Select(c => new CitaIncidenciaClave
                    {
                        ClaveInc = c
                    })
                    .ToList()
            };

            _context.Add(incidencia);
            await _context.SaveChangesAsync(ct);

            return incidencia;
        }



        public async Task MarcaArchivoCargado(MarcaArchivoCargadoDto dto, CancellationToken ct = default)
        {
            // Secuencia de validaciones.
            var ctx = new IncidenciaSolicitaUrlValidatorContext { IdCita = dto.CitaId, Dto = dto };
            var result = await _solicitaUrlValidator.ValidateAsync(ctx, ct);

            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            // Cambia la bandera de la carga del archivo. 
            await _context.CitasIncidencias
            .Where(c => c.Id == dto.IncidenciaId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.ArchivoCargado, _ => dto.Cargado));
        }

        public async Task<string> SolicitaUrlConSessionParaUploadMasivo(SolicitaUrlSessionEvidenciasMasivaDto dto, CancellationToken ct = default) {

            var bucket = "deposito-citas-efimero";
            var rutaArchivo = $"incidencias_fotograficas/cita_masiva/{dto.HashMasiva}/{DateTime.UtcNow:yyyyMMdd_HHmmss}_evidencias.zip";
            var contentType = "application/octet-stream";

            var credential = await GoogleCredential.GetApplicationDefaultAsync();
            var signer = UrlSigner.FromCredential(credential);

            var signedUrl = signer.Sign(
                bucket: bucket,
                objectName: rutaArchivo,
                duration: TimeSpan.FromMinutes(15),
                httpMethod: UrlSigner.ResumableHttpMethod,
                signingVersion: SigningVersion.V4
            );

            using var http = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, signedUrl);
            req.Headers.Add("x-goog-resumable", "start");
            req.Content = new ByteArrayContent(Array.Empty<byte>());
            req.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);


            var resp = await http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var sessionUrl = resp.Headers.Location?.ToString();

            long[] ids = dto.CitasId ?? Array.Empty<long>();

            if (ids.Length == 0) return null;

            await _context.CitasIncidencias
                .Where(c => c.HashMasivo == dto.HashMasiva)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.RutaArchivo, c => rutaArchivo));


            return sessionUrl;
        }
        public async Task<string> SolicitaUrlConSessionParaUpload(SolicitaUrlSessionEvidenciasDto dto, CancellationToken ct = default) 
        {


            // Secuencia de validaciones.
            var ctx = new IncidenciaSolicitaUrlValidatorContext { IdCita = dto.CitaId, Dto = dto };
            var result = await _solicitaUrlValidator.ValidateAsync(ctx, ct);

            if (!result.IsValid)
            {
                var errores = result.Errors.Select(e => $"{e.ErrorMessage}");
                throw new CitaException(string.Join("|", errores));
            }

            var bucket = "deposito-citas-efimero"; 
            var rutaArchivo = $"incidencias_fotograficas/cita_id/{dto.CitaId}/incidencia/{dto.IncidenciaId}/{DateTime.UtcNow:yyyyMMdd_HHmmss}_evidencias.zip";
            var contentType = "application/octet-stream";

            var credential = await GoogleCredential.GetApplicationDefaultAsync();
            var signer = UrlSigner.FromCredential(credential);

            var signedUrl = signer.Sign(
                bucket: bucket,
                objectName: rutaArchivo,
                duration: TimeSpan.FromMinutes(15),
                httpMethod: UrlSigner.ResumableHttpMethod,
                signingVersion: SigningVersion.V4
            );

            using var http = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, signedUrl);
            req.Headers.Add("x-goog-resumable", "start");
            req.Content = new ByteArrayContent(Array.Empty<byte>());
            req.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);


            var resp = await http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var sessionUrl = resp.Headers.Location?.ToString();

            // Agrega la ruta a la incidencia
            await _context.CitasIncidencias
            .Where(c => c.Id == dto.IncidenciaId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.RutaArchivo, _ => rutaArchivo));

            return sessionUrl;
        }
        public async Task<string> SolicitaUrlPrefirmadaParaDescarga(SolicitaUrlSessionEvidenciasDto dto, CancellationToken ct = default)
        {

            var incidencia = await _context.CitasIncidencias
                .Where(c => c.Id == dto.IncidenciaId && c.CitaId == dto.CitaId)
                .Select(c => new { c.RutaArchivo })
                .FirstOrDefaultAsync(ct);

            if (incidencia is null || string.IsNullOrWhiteSpace(incidencia.RutaArchivo))
                throw new CitaException("No se encontro la ruta del archivo de evidencias para la incidencia.");

            var bucket = "deposito-citas-efimero";
            var objectName = incidencia.RutaArchivo;
            var fileName = Path.GetFileName(objectName);
            var minutos = 10;

            var credential = await GoogleCredential.GetApplicationDefaultAsync();
            var signer = UrlSigner.FromCredential(credential);

            // Url prefirmada para la descarga del archivo
            var url = signer.Sign(
                bucket: bucket,
                objectName: objectName,
                duration: TimeSpan.FromMinutes(minutos),
                httpMethod: HttpMethod.Get,
                signingVersion: SigningVersion.V4
            );

            return url;
        }
    }
}

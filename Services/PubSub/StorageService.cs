using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using ApiProveedores.Services.Exceptions;

namespace ApiProveedores.Services.PubSub
{
    public class StorageService
    {
        private readonly string _bucket;

        public StorageService(IConfiguration config)
        {
            _bucket = config["GCP:BucketName"];
        }

        public async Task<string> UploadFilesAsync(Stream fileStream, string fileName)
        {
            var storage = await StorageClient.CreateAsync();

            var data = await storage.UploadObjectAsync(_bucket, fileName, null, fileStream);

            return data.Name;
        }

        public async Task<string> GenerateSignedUrlAsync(string objectUrl, TimeSpan? expiry = null)
        {
            if (string.IsNullOrWhiteSpace(objectUrl))
                throw new ApiProveedoresException("objectUrl inválida.");

            expiry ??= TimeSpan.FromMinutes(15);

            
            string bucket = null;
            string objectName = null;

            if (objectUrl.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
            {
                var rest = objectUrl.Substring(5);
                var idx = rest.IndexOf('/');
                if (idx <= 0)
                    throw new ApiProveedoresException("Formato de URL gs:// inválido.");

                bucket = rest.Substring(0, idx);
                objectName = rest.Substring(idx + 1);
            }
            else
            {
                var m = Regex.Match(objectUrl, @"https?://storage.googleapis.com/download/storage/v1/b/([^/]+)/o/(.+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    bucket = m.Groups[1].Value;
                    // object may be URL-encoded in this path
                    objectName = Uri.UnescapeDataString(m.Groups[2].Value);
                    // Si vienen query params incluidas en el match, quitarlo
                    var qIdx = objectName.IndexOf('?');
                    if (qIdx >= 0) objectName = objectName.Substring(0, qIdx);
                }
                else
                {
                    m = Regex.Match(objectUrl, @"https?://storage.googleapis.com/([^/]+)/(.+)", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        bucket = m.Groups[1].Value;
                        objectName = Uri.UnescapeDataString(m.Groups[2].Value);
                        // quitar query params por seguridad
                        var qIdx = objectName.IndexOf('?');
                        if (qIdx >= 0) objectName = objectName.Substring(0, qIdx);
                    }
                    else
                    {
                        m = Regex.Match(objectUrl, @"https?://([^.]+)\.storage\.googleapis\.com/(.+)", RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            bucket = m.Groups[1].Value;
                            objectName = Uri.UnescapeDataString(m.Groups[2].Value);
                            var qIdx = objectName.IndexOf('?');
                            if (qIdx >= 0) objectName = objectName.Substring(0, qIdx);
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(objectName))
                throw new ApiProveedoresException("No se pudo extraer bucket/objeto de la URL proporcionada.");

            try
            {
                // Obtener credenciales ADC
                var credential = await GoogleCredential.GetApplicationDefaultAsync();

                UrlSigner signer = null;

                // Si la credencial subyacente es una ServiceAccountCredential podemos firmar localmente
                if (credential.UnderlyingCredential is Google.Apis.Auth.OAuth2.ServiceAccountCredential)
                {
                    signer = UrlSigner.FromCredential(credential);
                }
                else
                {
                    // Intentar fallback a path de credenciales si existe
                    var path = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        signer = UrlSigner.FromServiceAccountPath(path);
                    }
                }

                if (signer == null)
                    throw new ApiProveedoresException("No se encontraron credenciales de cuenta de servicio para firmar las URLs. Configure GOOGLE_APPLICATION_CREDENTIALS apuntando al JSON de la cuenta de servicio o ejecute con credenciales de servicio.");

                var signedUrl = signer.Sign(bucket, objectName, expiry.Value, HttpMethod.Get);
                return signedUrl;
            }
            catch (Exception ex)
            {
                throw new ApiProveedoresException($"No se pudo generar URL firmada: {ex.Message}");
            }
        }
    }
}

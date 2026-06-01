using ApiProveedores.Services.Exceptions;
using Google.Cloud.Iam.Credentials.V1;
using Google.Cloud.Storage.V1;
using Google.Protobuf;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ApiProveedores.Services.PubSub
{
    public class StorageService
    {
        private readonly string _bucket;
        private readonly string _baseFolder;
        private readonly string _reportesFolder;

        public StorageService(IConfiguration config)
        {
            _bucket = config["GCP:BucketName"] ?? string.Empty;
            _baseFolder = config["GCP:BaseFolder"] ?? string.Empty;
            _reportesFolder = config["GCP:ReportesFolder"] ?? string.Empty;
        }

        public async Task<string> UploadFilesAsync(Stream fileStream, string fileName, string typeFile, bool esReporte = false)
        {
            var storage = await StorageClient.CreateAsync();

            var objectName = string.Empty;

            if(esReporte)
            {
                objectName = $"{_reportesFolder}/{DateTime.Now:yyyy/MM}/{fileName}";
            }
            else
            {
                objectName = $"{_baseFolder}/{typeFile}/{DateTime.Now:yyyy/MM}/{fileName}";
            }

            try
            {
                var data = await storage.UploadObjectAsync(_bucket, objectName, null, fileStream);
                return data.Name;

            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }


        }

        public async Task<string> GenerateSignedUrlAsync(string objectUrl, TimeSpan? expiry = null)
        {
            if (string.IsNullOrWhiteSpace(objectUrl))
                throw new ApiProveedoresException("objectUrl inválida.");

            expiry ??= TimeSpan.FromMinutes(45);

            try
            {

                if (string.IsNullOrEmpty(_bucket) || string.IsNullOrEmpty(objectUrl))
                    throw new ApiProveedoresException("No se pudo extraer bucket/objeto de la URL proporcionada.");

                var serviceAccount = "portal-proveedores-api-runtime@marti-deportes-desarrollo.iam.gserviceaccount.com";

                var now = DateTime.UtcNow;
                var datestamp = now.ToString("yyyyMMdd");
                var timestamp = now.ToString("yyyyMMdd'T'HHmmss'Z'");
                var expires = ((int)expiry.Value.TotalSeconds).ToString();

                var credentialScope = $"{datestamp}/auto/storage/goog4_request";

                string EncodePath(string path)
                {
                    return string.Join("/", path.Split('/')
                        .Select(p => Uri.EscapeDataString(p)));
                }

                var canonicalUri = "/" + EncodePath(objectUrl);
                var host = $"{_bucket}.storage.googleapis.com";

                var queryParams = new SortedDictionary<string, string>
                {
                    { "X-Goog-Algorithm", "GOOG4-RSA-SHA256" },
                    { "X-Goog-Credential", $"{serviceAccount}/{credentialScope}" },
                    { "X-Goog-Date", timestamp },
                    { "X-Goog-Expires", expires },
                    { "X-Goog-SignedHeaders", "host" }
                };

                var canonicalQueryString = string.Join("&",
                    queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

                var canonicalHeaders = $"host:{host}\n";
                var signedHeaders = "host";
                var payloadHash = "UNSIGNED-PAYLOAD";

                var canonicalRequest = $"GET\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonicalRequest));
                var hashedRequest = BitConverter.ToString(hash).Replace("-", "").ToLower();

                var stringToSing = $"GOOG4-RSA-SHA256\n{timestamp}\n{credentialScope}\n{hashedRequest}";

                var client = await IAMCredentialsClient.CreateAsync();

                var request = new SignBlobRequest
                {
                    Name = $"projects/-/serviceAccounts/{serviceAccount}",
                    Payload = ByteString.CopyFromUtf8(stringToSing)
                };

                var response = await client.SignBlobAsync(request);

                var signature = BitConverter.ToString(response.SignedBlob.ToByteArray())
                    .Replace("-", "")
                    .ToLower();

                var finalUrl = $"https://{_bucket}.storage.googleapis.com/{EncodePath(objectUrl)}?{canonicalQueryString}&X-Goog-Signature={signature}";

                return finalUrl;
            }
            catch (Exception ex)
            {
                throw new ApiProveedoresException($"No se pudo generar URL firmada: {ex.Message}");
            }
        }
    }
}

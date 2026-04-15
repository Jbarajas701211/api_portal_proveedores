using ApiProveedores.Services.Exceptions;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Iam.Credentials.V1;
using Google.Cloud.Storage.V1;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace ApiProveedores.Services.PubSub
{
    public class StorageService
    {
        private readonly string _bucket;
        private readonly string _baseFolder;

        public StorageService(IConfiguration config)
        {
            _bucket = config["GCP:BucketName"] ?? string.Empty;
            _baseFolder = config["GCP:BaseFolder"] ?? string.Empty;
        }

        public async Task<string> UploadFilesAsync(Stream fileStream, string fileName, string typeFile)
        {
            var storage = await StorageClient.CreateAsync();

            var objectName = $"{_baseFolder}/{typeFile}/{DateTime.Now:yyyy/MM}/{fileName}";

            var data = await storage.UploadObjectAsync(_bucket, objectName, null, fileStream);

            return data.Name;
        }

        public async Task<string> GenerateSignedUrlAsync(string objectUrl, TimeSpan? expiry = null)
        {
            if (string.IsNullOrWhiteSpace(objectUrl))
                throw new ApiProveedoresException("objectUrl inválida.");

            expiry ??= TimeSpan.FromMinutes(15);

            
            string bucket = null;
            string objectName = null;

            if (objectUrl.Contains("/download/storage/v1/b/"))
            {
                var match = Regex.Match(objectUrl,
                    @"https://storage.googleapis.com/download/storage/v1/b/([^/]+)/o/([^?]+)");

                if (match.Success)
                {
                    var bucketFix = match.Groups[1].Value;
                    var objectFix = Uri.UnescapeDataString(match.Groups[2].Value);

                    objectUrl = $"https://storage.googleapis.com/{bucketFix}/{objectFix}";
                }
            }

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
                var uri = new Uri(objectUrl);
                var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);

                if (segments.Length < 2)
                    throw new ApiProveedoresException("URL inválida.");

                bucket = segments[0];
                objectName = Uri.UnescapeDataString(segments[1]);
            }

            if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(objectName))
                throw new ApiProveedoresException("No se pudo extraer bucket/objeto de la URL proporcionada.");

            try
            {
                var serviceAccount = "proveedores-portal-cs@proveedores-491118.iam.gserviceaccount.com";

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

                var canonicalUri = "/" + EncodePath(objectName);
                var host = $"{bucket}.storage.googleapis.com";

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

                var finalUrl = $"https://{bucket}.storage.googleapis.com/{EncodePath(objectName)}?{canonicalQueryString}&X-Goog-Signature={signature}";

                return finalUrl;
            }
            catch (Exception ex)
            {
                throw new ApiProveedoresException($"No se pudo generar URL firmada: {ex.Message}");
            }
        }
    }
}

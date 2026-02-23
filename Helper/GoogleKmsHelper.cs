namespace ApiProveedores.Helper
{
    using Google.Cloud.Kms.V1;
    using Google.Protobuf;
    using System;
    using System.Threading.Tasks;


    public class GoogleKmsHelper
    {
        private readonly KeyManagementServiceClient _client;
        private readonly CryptoKeyName _cryptoKeyName;

        public GoogleKmsHelper(string projectId, string locationId, string keyRingId, string cryptoKeyId)
        {
            _client = KeyManagementServiceClient.Create();
            _cryptoKeyName = CryptoKeyName.FromProjectLocationKeyRingCryptoKey(projectId, locationId, keyRingId, cryptoKeyId);
        }

        public async Task<string> EncryptAsync(string plainText)
        {
            ByteString plaintextBytes = ByteString.CopyFromUtf8(plainText);
            EncryptResponse result = await _client.EncryptAsync(_cryptoKeyName, plaintextBytes);
            return Convert.ToBase64String(result.Ciphertext.ToByteArray());
        }

        public async Task<string> DecryptAsync(string cipherTextBase64)
        {
            ByteString cipherBytes = ByteString.CopyFrom(Convert.FromBase64String(cipherTextBase64));
            DecryptResponse result = await _client.DecryptAsync(_cryptoKeyName, cipherBytes);
            return result.Plaintext.ToStringUtf8();
        }
    }


}

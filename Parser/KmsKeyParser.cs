using System;

namespace ApiProveedores.Parser
{
    public static class KmsKeyParser
    {
        public static (string projectId, string locationId, string keyRingId, string keyId) ParseKeyResource(string keyResource)
        {
            var parts = keyResource.Split('/');

            if (parts.Length != 8 ||
                parts[0] != "projects" ||
                parts[2] != "locations" ||
                parts[4] != "keyRings" ||
                parts[6] != "cryptoKeys")
            {
                throw new ArgumentException("Formato de clave KMS inválido.");
            }

            return (parts[1], parts[3], parts[5], parts[7]);
        }
    }

}

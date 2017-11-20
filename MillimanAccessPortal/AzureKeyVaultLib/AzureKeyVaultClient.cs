using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzureKeyVaultLib
{
    class AzureKeyVaultClient
    {
        public ClientAssertionCertificate AssertionCert { get; set; }
        public string CertificateThumbprint { get; set; }
        public string ClientId { get; set; }

        public AzureKeyVaultClient(string _thumbprint, string _clientId)
        {
            var clientAssertionCertPfx = CertificateHelper.FindCertificateByThumbprint(CertificateThumbprint);
            AssertionCert = new ClientAssertionCertificate(ClientId, clientAssertionCertPfx);
        }
        
        public async Task<string> GetAccessToken(string authority, string resource, string scope)
        {
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var result = await context.AcquireTokenAsync(resource, AssertionCert);

            return result.AccessToken;
        }

        public async Task<string> GetNamedSecret(string secretURL)
        {
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken));
            var sec = await kv.GetSecretAsync(secretURL);

            return sec.Value;
        }
    }

 }

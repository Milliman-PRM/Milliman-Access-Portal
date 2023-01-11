using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerRegistry.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudResourceLib
{
    [Flags]
    public enum CredentialScope
    {
        ContainerRegistry = 1,
        ContainerInstance = 2,
        Storage = 4
    }

    public class AzureClientCredential
    {
        public CredentialScope Scope { get; set; }
        public string TenantId { get; private init; }
        public string ClientId { get; private init; }
        public string ClientSecret { get; private init; }

        // An instance of this class can not be constructed directly
        private AzureClientCredential() { }

        /// <summary>
        /// The only way to instantiate this class.  This method ensures that all properties are provided. 
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <returns></returns>
        public static AzureClientCredential NewInstance(CredentialScope scope, string tenantId, string clientId, string clientSecret)
        {
            if (string.IsNullOrEmpty(tenantId) ||
                string.IsNullOrEmpty(clientId) ||
                string.IsNullOrEmpty(clientSecret))
            {
                return null;
            }

            return new AzureClientCredential
            {
                Scope = scope,
                TenantId = tenantId,
                ClientId = clientId,
                ClientSecret = clientSecret
            };
        }
    }

    public static class CloudResourceApi
    {
        private static ArmClient _storageClient = null;
        private static ArmClient _containerRegistryClient = null;
        private static ArmClient _containerInstanceClient = null;

        private static Action<AzureClientCredential> AssertValid = credential =>
        {
            if (credential is null) throw new CredentialUnavailableException("Storage credential not available");
        };

        /// <summary>
        /// Initializes this api with Azure client credentials
        /// </summary>
        /// <param name="credentials">A collection of credentials</param>
        public static void InitClients(IEnumerable<AzureClientCredential> credentials)
        {
            _containerRegistryClient = _containerInstanceClient = _storageClient = null;

            #region Make sure no scope is specified for multiple credentials
            CredentialScope[] possibleScopeValues = Enum.GetValues<CredentialScope>();
            IEnumerable<CredentialScope> allProvidedScopes = credentials.Where(c => c is not null)
                                                                        .SelectMany(c => possibleScopeValues.Where(v => (c.Scope & v) == v));
            var badScopes = allProvidedScopes.Where(s => allProvidedScopes.Count(v => v == s) > 1).Distinct();
            if (badScopes.Any())
            {
                // TODO Log this with Serilog and throw
                Console.WriteLine($"Scope(s) <{string.Join(",", badScopes)}> cannot be requested for multiple cloud credentials");
            }
            #endregion

            foreach (var credential in credentials.Where(c => c is not null))
            {
                int scope = (int)credential.Scope;
                // Each credential can have more than one scope
                if ((credential.Scope & CredentialScope.ContainerRegistry) == CredentialScope.ContainerRegistry)
                {
                    _containerRegistryClient = new ArmClient(new ClientSecretCredential(credential.TenantId, credential.ClientId, credential.ClientSecret));
                }

                if ((credential.Scope & CredentialScope.ContainerInstance) == CredentialScope.ContainerInstance)
                {
                    _containerInstanceClient = new ArmClient(new ClientSecretCredential(credential.TenantId, credential.ClientId, credential.ClientSecret));
                }

                if ((credential.Scope & CredentialScope.Storage) == CredentialScope.Storage)
                {
                    _storageClient = new ArmClient(new ClientSecretCredential(credential.TenantId, credential.ClientId, credential.ClientSecret));
                }
            }
        }

        // TODO insert real operations here

    }
}

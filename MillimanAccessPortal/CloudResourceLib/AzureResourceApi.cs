using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerRegistry.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.Storage.Files.Shares;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.ResourceManager.Storage.Models;
using System.IO;

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

    // Things to learn:
    // - Permissions on accounts/shares/directories
    // - Tagging
    // - Difference in storage account type
    // - Difference in SKU
    // Do we want to allow customers to get direct access to the storage accounts?

    public static class AzureResourceApi
    {
        private static ArmClient _storageClient = null;
        private static ArmClient _containerRegistryClient = null;
        private static ArmClient _containerInstanceClient = null;

        private static Action<AzureClientCredential> AssertValid = credential =>
        {
            if (credential is null)
            {
                throw new CredentialUnavailableException("Storage credential not available");
            }
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
                string msg = $"Scope(s) <{string.Join(",", badScopes)}> cannot be requested for multiple cloud credentials";
                Log.Warning(msg);
                throw new ApplicationException(msg);
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

        #region Storage operations
        public static async Task CreateNewStorage(Guid ContentItemId)
        {
            SubscriptionResource subscription = await _storageClient.GetDefaultSubscriptionAsync();
            ResourceGroupResource containerTestingResourceGroup = subscription.GetResourceGroup("ContainerTestingResourceGroup");

            StorageSku sku = new StorageSku(StorageSkuName.StandardGrs);
            StorageKind kind = StorageKind.Storage;
            AzureLocation location = AzureLocation.EastUS;
            StorageAccountCreateOrUpdateContent creationParams = new StorageAccountCreateOrUpdateContent(sku, kind, location);
            StorageAccountCollection accountCollection = containerTestingResourceGroup.GetStorageAccounts();
            string newStorageAccountName = "TomTesting";
            ArmOperation<StorageAccountResource> accountCreateOperation = await accountCollection.CreateOrUpdateAsync(WaitUntil.Completed, newStorageAccountName, creationParams);
            StorageAccountResource storageAccount = accountCreateOperation.Value;


            accountCollection = containerTestingResourceGroup.GetStorageAccounts();

            //var containerGroups = containerTestingResourceGroup.GetContainerGroups();
        }

        // TODO determine return values here...
        public static async Task<FileShareResource> CreateFileShare(string storageAccountName, string fileShareName)
        {
            SubscriptionResource subscription = await _storageClient.GetDefaultSubscriptionAsync();
            ResourceGroupResource resourceGroup = subscription.GetResourceGroup("ContainerTestingResourceGroup");
            StorageAccountResource targetedStorageAccount = await resourceGroup.GetStorageAccountAsync(storageAccountName);

            FileServiceResource fileService = await targetedStorageAccount.GetFileService().GetAsync();
            FileShareCollection fileShareCollection = fileService.GetFileShares();
            ArmOperation<FileShareResource> fileShareCreateOperation = await fileShareCollection.CreateOrUpdateAsync(WaitUntil.Started, fileShareName, new FileShareData());
            FileShareResource fileShare = await fileShareCreateOperation.WaitForCompletionAsync();
            return fileShare;
        }

        public static async Task CreateFileShareDirectory(string storageAccountName, string fileShareName, string directoryName)
        {
            SubscriptionResource subscription = await _storageClient.GetDefaultSubscriptionAsync();
            ResourceGroupResource resourceGroup = subscription.GetResourceGroup("ContainerTestingResourceGroup");
            StorageAccountResource targetedStorageAccount = await resourceGroup.GetStorageAccountAsync(storageAccountName);
            var storageAccountKeys = targetedStorageAccount.GetKeys();

            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={storageAccountKeys.FirstOrDefault().Value};EndpointSuffix=core.windows.net";
            ShareClient shareClient = new ShareClient(connectionString, fileShareName);
            ShareDirectoryClient directoryClient = shareClient.GetDirectoryClient(directoryName);
            directoryClient.Create();
        }

        public static async Task UploadToFileShare(string storageAccountName, string fileShareName, string directoryName, string fileName, string localFilePath)
        {
            SubscriptionResource subscription = await _storageClient.GetDefaultSubscriptionAsync();
            ResourceGroupResource resourceGroup = subscription.GetResourceGroup("ContainerTestingResourceGroup");
            StorageAccountResource targetedStorageAccount = await resourceGroup.GetStorageAccountAsync(storageAccountName);
            var storageAccountKeys = targetedStorageAccount.GetKeys();

            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={storageAccountKeys.FirstOrDefault().Value};EndpointSuffix=core.windows.net";
            ShareClient shareClient = new ShareClient(connectionString, fileShareName);
            ShareDirectoryClient directoryClient = shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync();

            ShareFileClient fileClient = directoryClient.GetFileClient(fileName);
            using (FileStream stream = File.OpenRead(localFilePath))
            {
                fileClient.Create(stream.Length);
                fileClient.UploadRange(new HttpRange(0, stream.Length), stream);
            }
        }

        #endregion
    }
}

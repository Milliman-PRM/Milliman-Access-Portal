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
        /// <summary>
        /// Create a Storage Account for a given Client.
        /// </summary>
        /// <param name="clientName"></param>
        /// <param name="contentItemId"></param>
        /// <returns></returns>
        public static async Task CreateNewStorage(string clientName, Guid contentItemId)
        {
            SubscriptionResource subscription = await _storageClient.GetDefaultSubscriptionAsync();
            ResourceGroupResource containerTestingResourceGroup = subscription.GetResourceGroup(clientName);

            ResourceGroupCollection allResourceGroups = subscription.GetResourceGroups();

            ArmOperation<ResourceGroupResource> createResourceGroupOperation = allResourceGroups.CreateOrUpdate(WaitUntil.Completed, $"map-client-{ClientId}", new ResourceGroupData(AzureLocation.EastUS) {Tags = {{ "ClientId", ClientId.ToString() } } });
            ResourceGroupResource newResourceGroup = createResourceGroupOperation.WaitForCompletion();

            StorageSku sku = new StorageSku(StorageSkuName.StandardGrs);
            StorageKind kind = StorageKind.StorageV2;
            AzureLocation location = AzureLocation.EastUS;
            StorageAccountCreateOrUpdateContent creationParams = new StorageAccountCreateOrUpdateContent(sku, kind, location);
            StorageAccountCollection accountCollection = containerTestingResourceGroup.GetStorageAccounts();
            ArmOperation<StorageAccountResource> accountCreateOperation = await accountCollection.CreateOrUpdateAsync(WaitUntil.Completed, contentItemId.ToString(), creationParams);
            StorageAccountResource storageAccount = accountCreateOperation.Value;
        }
        #endregion

        #region Share Operations
        /// <summary>
        /// Creates an Azure File Share.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="storageAccountName"></param>
        /// <param name="fileShareName"></param>
        /// <returns></returns>
        public static async Task CreateFileShare(string resourceGroupName, string storageAccountName, string fileShareName)
        {
            try
            {
                StorageAccountResource targetedStorageAccount = await FetchStorageAccount(resourceGroupName, storageAccountName);
                FileServiceResource fileService = await targetedStorageAccount.GetFileService().GetAsync();
                FileShareCollection fileShareCollection = fileService.GetFileShares();
                ArmOperation<FileShareResource> fileShareCreateOperation = await fileShareCollection.CreateOrUpdateAsync(WaitUntil.Started, fileShareName, new FileShareData());

                await fileShareCreateOperation.WaitForCompletionAsync(); // Do assignment and return??
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating new Azure File Share.");
                throw;
            }
        }

        /// <summary>
        /// Removes a File Share.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="storageAccountName"></param>
        /// <param name="fileShareName"></param>
        /// <returns></returns>
        public static async Task RemoveFileShare(string resourceGroupName, string storageAccountName, string fileShareName)
        {
            try
            {
                StorageAccountResource targetedStorageAccount = await FetchStorageAccount(resourceGroupName, storageAccountName);
                var storageAccountKeys = targetedStorageAccount.GetKeys();
                string connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={storageAccountKeys.FirstOrDefault().Value};EndpointSuffix=core.windows.net";
                ShareClient shareClient = new ShareClient(connectionString, fileShareName);
                await shareClient.DeleteIfExistsAsync(); // May take several minutes.
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error removing Azure File Share.");
                throw;
            }
        }

        /// <summary>
        /// Removes all files and directories within a share, without deleting the share itself.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="storageAccountName"></param>
        /// <param name="fileShareName"></param>
        /// <param name="directoryName"></param>
        /// <returns></returns>
        public static async Task ClearFileShare(string resourceGroupName, string storageAccountName, string fileShareName, string directoryName)
        {
            StorageAccountResource targetedStorageAccount = await FetchStorageAccount(resourceGroupName, storageAccountName);

            var storageAccountKeys = targetedStorageAccount.GetKeys();
            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={storageAccountKeys.FirstOrDefault().Value};EndpointSuffix=core.windows.net";
            ShareClient shareClient = new ShareClient(connectionString, fileShareName);
            var rootDirectoryClient = shareClient.GetRootDirectoryClient();
            
            // TODO Figure out strategy.
            // Strategy #1: Remove directories recursively
            // Strategy #2: Remove File Share completely, then re-create.
            // Strategy #3??: Maybe there's a built in package function to do this...
        }

        /// <summary>
        /// Creates a directory within a share.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="storageAccountName"></param>
        /// <param name="fileShareName"></param>
        /// <param name="directoryName"></param>
        /// <returns></returns>
        public static async Task CreateFileShareDirectory(string resourceGroupName, string storageAccountName, string fileShareName, string directoryName)
        {
            try
            {
                StorageAccountResource targetedStorageAccount = await FetchStorageAccount(resourceGroupName, storageAccountName);
                var storageAccountKeys = targetedStorageAccount.GetKeys();
                string connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={storageAccountKeys.FirstOrDefault().Value};EndpointSuffix=core.windows.net";
                ShareClient shareClient = new ShareClient(connectionString, fileShareName);
                await shareClient.CreateDirectoryAsync(directoryName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating directory within Azure File Share.");
                throw;
            }
        }

        /// <summary>
        /// Uploads a file to an internal directory within a File Share.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="storageAccountName"></param>
        /// <param name="fileShareName"></param>
        /// <param name="directoryName"></param>
        /// <param name="fileName"></param>
        /// <param name="localFilePath"></param>
        /// <returns></returns>
        public static async Task UploadToFileShare(string resourceGroupName, string storageAccountName, string fileShareName, string directoryName, string fileName, string localFilePath)
        {
            try
            {
                StorageAccountResource targetedStorageAccount = await FetchStorageAccount(resourceGroupName, storageAccountName);
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
            catch (Exception ex)
            {
                Log.Error(ex, "Error uploading file to Azure File Share.");
                throw;
            }
        }

        /// <summary>
        /// Redundant logic for fetching a Storage Account using credentials.
        /// </summary>
        /// <param name="resourceGroupName"></param>
        /// <param name="storageAccountName"></param>
        /// <returns></returns>
        private static async Task<StorageAccountResource> FetchStorageAccount(string resourceGroupName, string storageAccountName)
        {
            try
            {
                SubscriptionResource subscription = await _storageClient.GetDefaultSubscriptionAsync();
                ResourceGroupResource resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName);
                StorageAccountResource storageAccount = await resourceGroup.GetStorageAccountAsync(storageAccountName);
                return storageAccount;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetcing Azure subscription, resource group, or storage account info.");
                throw;
            }
        }
        #endregion
    }
}

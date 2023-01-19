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
        public static async Task CreateNewStorage(Guid ClientId, Guid ContentItemId)
        {
            //SubscriptionResource subscription = await _storageClient.GetDefaultSubscriptionAsync();
            SubscriptionResource subscription = _storageClient.GetSubscriptions().Single(s => s.HasData && s.Data.DisplayName == "");

            ResourceGroupCollection allResourceGroups = subscription.GetResourceGroups();

            ArmOperation<ResourceGroupResource> createResourceGroupOperation = allResourceGroups.CreateOrUpdate(WaitUntil.Completed, $"map-client-{ClientId}", new ResourceGroupData(AzureLocation.EastUS) {Tags = {{ "ClientId", ClientId.ToString() } } });
            ResourceGroupResource newResourceGroup = createResourceGroupOperation.WaitForCompletion();

            StorageSku sku = new StorageSku(StorageSkuName.StandardGrs);

            StorageAccountCreateOrUpdateContent creationParams = new StorageAccountCreateOrUpdateContent(sku, StorageKind.StorageV2, newResourceGroup.Data.Location);
            StorageAccountCollection accountCollection = newResourceGroup.GetStorageAccounts();
            string newStorageAccountName = "TomTesting";
            ArmOperation<StorageAccountResource> accountCreateOperation = await accountCollection.CreateOrUpdateAsync(WaitUntil.Completed, newStorageAccountName, creationParams);
            StorageAccountResource storageAccount = accountCreateOperation.Value;

            accountCollection = newResourceGroup.GetStorageAccounts();

            // TODO Figure out if this deletes contained resources too
            newResourceGroup.Delete(WaitUntil.Completed);

            //var containerGroups = containerTestingResourceGroup.GetContainerGroups();

            //ContainerGroupData grData = new ContainerGroupData(AzureLocation.EastUS, newContainers, ContainerInstanceOperatingSystemType.Windows)
            //{

            //    // I'd like to add more properties here
            //};

            //var op = containerGroups.CreateOrUpdate(WaitUntil.Completed, "TomTestContainerGroup", grData);

#if false  // Evan's sample
using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.ResourceManager.Storage.Models;
using Azure.ResourceManager.Resources;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure;
using System.Xml.Linq;
using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Azure;


            #region Credentials and user data

// Secrets
string azureTenantId = "ecd586dd-352e-4830-8c8f-b6ca8be7ca04";
string azureClientId = "efd34fda-6721-4f53-a186-b5014ce79ce6";
string azureClientSecret = "IoD7Q~5FrwghbqL0Q6baLsd.RXV-A6r1eAjaI";
string resourceGroupName = "ContainerTestingResourceGroup";

// File upload information
string fileName = "file.txt";
string filePath = @"C:\path\to\file\here\file.txt";

            #endregion

            #region Azure.ResourceManager.Storage territory

// Create ARMClient, fetch subscription resource and resource group
var credential = new ClientSecretCredential(azureTenantId, azureClientId, azureClientSecret);
ArmClient azureResourceManagementClient = new ArmClient(credential);
SubscriptionResource subscription = await azureResourceManagementClient.GetDefaultSubscriptionAsync();
ResourceGroupResource resourceGroup = subscription.GetResourceGroup(resourceGroupName);


// Create new storage accountand add it to list of storage accounts for resource group
StorageSku sku = new StorageSku(StorageSkuName.StandardGrs);
StorageKind kind = StorageKind.Storage;
AzureLocation location = AzureLocation.EastUS;
StorageAccountCreateOrUpdateContent creationParams = new StorageAccountCreateOrUpdateContent(sku, kind, location);
StorageAccountCollection accountCollection = resourceGroup.GetStorageAccounts();
string newStorageAccountName = "createdbyapi";
ArmOperation<StorageAccountResource> accountCreateOperation = await accountCollection.CreateOrUpdateAsync(WaitUntil.Completed, newStorageAccountName, creationParams);
StorageAccountResource storageAccount = accountCreateOperation.Value;

// Store Storage Account Keys
var storageAccountKeys = storageAccount.GetKeys();

// ResourceManager Create File Share
FileServiceResource fileService = await storageAccount.GetFileService().GetAsync();
FileShareCollection fileShareCollection = fileService.GetFileShares();

string newFileShareName = "newshare";
FileShareData fileShareData = new FileShareData();

ArmOperation<FileShareResource> fileShareCreateOperation = await fileShareCollection.CreateOrUpdateAsync(WaitUntil.Started, newFileShareName, fileShareData);
FileShareResource fileShare = await fileShareCreateOperation.WaitForCompletionAsync();

            #endregion

            #region Azure.Storage.Files.Shares API territory

// Create a File Share
string connectionString = $"DefaultEndpointsProtocol=https;AccountName={newStorageAccountName};AccountKey={storageAccountKeys.FirstOrDefault().Value};EndpointSuffix=core.windows.net"; // Construct storage account connection string manually, as there doesn't appear to be a way to get it directly from either API
ShareClient shareClient = new ShareClient(connectionString, newFileShareName);

// Create a directory and create a client
ShareDirectoryClient directoryClient = shareClient.GetDirectoryClient("map");
directoryClient.Create();

// Create file client using directoryClient and upload file to share
ShareFileClient fileClient = directoryClient.GetFileClient(fileName);
using (FileStream stream = File.OpenRead(filePath))
{
    fileClient.Create(stream.Length);
    fileClient.UploadRange(new HttpRange(0, stream.Length), stream);
}

            #endregion

// Things to learn:
// - Permissions on accounts/shares/directories
// - Tagging
// - Difference in storage account type
// - Difference in SKU
// Do we want to allow customers to get direct access to the storage accounts?

#endif
        }

        #endregion
    }
}

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
using System.Text;

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

    public class AzureResourceApi
    {
        private static ArmClient _storageClient = null;
        private static ArmClient _containerRegistryClient = null;
        private static ArmClient _containerInstanceClient = null;

        private static AzureLocation _resourceLocation;

        private SubscriptionResource _storageSubscription = null;
        private ResourceGroupResource _storageResourceGroup = null;
        private StorageAccountResource _storageAccount = null;
        private FileServiceResource _fileService = null;

        private Func<Guid,string> StorageAccountName = clientId => $"client{clientId.ToString("N").Substring(0, 16)}";
        private Func<Guid,string> StorageResourceGroupName = clientId => $"map-client-{clientId}";

        public AzureResourceApi(Guid clientId, CredentialScope scope)
        {
            // Verify that the api is initialized with credentials for the requested scope
            AssertValid(scope);

            switch (scope)
            {
                case CredentialScope.ContainerInstance:
                    break;

                case CredentialScope.ContainerRegistry: 
                    break;

                case CredentialScope.Storage:
                    _storageSubscription = _storageClient.GetDefaultSubscription();
                    ResourceGroupCollection allResourceGroups = _storageSubscription.GetResourceGroups();

                    ArmOperation<ResourceGroupResource> createResourceGroupOperation = allResourceGroups.CreateOrUpdate(WaitUntil.Completed,
                                                                                                                        StorageResourceGroupName(clientId),
                                                                                                                        new ResourceGroupData(_resourceLocation)
                                                                                                                        {
                                                                                                                            Tags =
                                                                                                                            {
                                                                                                                                { "ClientId", clientId.ToString() },
                                                                                                                                // TODO more
                                                                                                                            },
                                                                                                                            // ManagedBy = ???,
                                                                                                                        });
                    _storageResourceGroup = createResourceGroupOperation.WaitForCompletion();

                    StorageAccountCollection storageAccountCollection = _storageResourceGroup.GetStorageAccounts();

                    StorageSku sku = new(StorageSkuName.StandardGrs);
                    StorageKind kind = StorageKind.StorageV2;
                    StorageAccountCreateOrUpdateContent creationParams = new StorageAccountCreateOrUpdateContent(sku, kind, _resourceLocation) 
                        { 
                            // TODO Set any properties here?
                        };
                    ArmOperation<StorageAccountResource> storageAccountCreateOperation = storageAccountCollection.CreateOrUpdate(WaitUntil.Completed, StorageAccountName(clientId), creationParams);
                    _storageAccount = storageAccountCreateOperation.Value;

                    _fileService = _storageAccount.GetFileService().Get();
                    break;
            }
        }

        /// <summary>
        /// If invoked with multiple combined CredentialScope flag values, will only validate for one of them
        /// </summary>
        private static Action<CredentialScope> AssertValid = scope =>
        {
            ArmClient armClient = scope switch
            {
                var s when (s & CredentialScope.ContainerInstance) == CredentialScope.ContainerInstance => _containerInstanceClient,
                var s when (s & CredentialScope.ContainerRegistry) == CredentialScope.ContainerRegistry => _containerRegistryClient,
                var s when (s & CredentialScope.Storage) == CredentialScope.Storage => _storageClient,
                _ => throw new NotImplementedException($"Attempt to assert valid client for unsupported CredentialScope value {scope}")
            };

            if (armClient is null)
            {
                throw new CredentialUnavailableException($"{scope} credential not initialized");
            }
        };

        /// <summary>
        /// Initializes this api with Azure client credentials
        /// </summary>
        /// <param name="credentials">A collection of credentials</param>
        /// <param name="location">Name of an Azure public cloud location.  See <see cref="https://learn.microsoft.com/en-us/dotnet/api/azure.core.azurelocation?view=azure-dotnet#properties"/></param>
        public static void InitClients(IEnumerable<AzureClientCredential> credentials, string location)
        {
            _containerRegistryClient = _containerInstanceClient = _storageClient = null;
            _resourceLocation = location;  // uses implicit cast operator in AzureLocation

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

        #region Share Operations
        /// <summary>
        /// Generates a unique name for use when creating a new file share.  Salt is required because it is possible for 
        /// multiple shares to exist for the same semantic purpose due to extended time required to delete a previous share
        /// </summary>
        /// <param name="contentItemId"></param>
        /// <param name="name"></param>
        /// <param name="isPreview">true if the new share is to support a preview (pre-live) content item</param>
        /// <returns></returns>
        private string GenerateUniqueNewShareName(Guid contentItemId, string name, bool isPreview)
        {
            const int saltLength = 4;
            const string validSaltChars = "abcdefghijklmnopqrstuvwxyz1234567890";
            string returnValue = string.Empty;

            List<string> fileShareNames = GetExistingShareNamesForContent(contentItemId, name, isPreview);
            Log.Debug($"Found shares with names:{string.Join("", fileShareNames.Select(n => $"{Environment.NewLine}    {n}"))}");  // temporary or improve

            do
            {
                StringBuilder res = new StringBuilder();
                Random rnd = new Random();
                for (int c = 0; c < saltLength; c++)
                {
                    res.Append(validSaltChars[rnd.Next(validSaltChars.Length)]);
                }
                returnValue = $"content-{contentItemId.ToString("N")}-{name}-{res.ToString()}{(isPreview ? "-preview" : "")}";
            }
            while (fileShareNames.Contains(returnValue));

            return returnValue;
        }

        /// <summary>
        /// If any share is currently being deleted, the name of that share will not be return by this method
        /// </summary>
        /// <param name="contentItemId"></param>
        /// <param name="name"></param>
        /// <param name="isPreview"></param>
        /// <returns></returns>
        private List<string> GetExistingShareNamesForContent(Guid contentItemId, string name, bool isPreview)
        {
            List<string> fileShareNames = _fileService.GetFileShares()
                                                      .Select(s => s.Data.Name)
                                                      .Where(n => n.StartsWith($"content-{contentItemId.ToString("N")}-{name}-") &&
                                                                  n.EndsWith(isPreview ? "-preview" : ""))
                                                      .ToList();

            return fileShareNames;
        }

        public bool FindExistingShareByName(Guid contentItemId, string name, bool isPreview, out FileShareResource existingShareResource)
        {
            FileShareCollection fileShareCollection = _fileService.GetFileShares();

            string nameStartsWith = $"content-{contentItemId.ToString("N")}-{name}-";

            // TODO What to do if more than one matches the following?
            // That would likely mean at least one is in the process of being deleted and a newer one has been created
            FileShareResource foundShare = fileShareCollection.SingleOrDefault(s => s.Data.Name.StartsWith(nameStartsWith) &&
                                                                                    (isPreview ? s.Data.Name.EndsWith("-preview") : true) &&
                                                                                    !(s.Data.IsDeleted.HasValue && s.Data.IsDeleted.Value));

            if (foundShare != null)
            {
                existingShareResource = foundShare;
                return true;
            }
            else
            {
                existingShareResource = null;
                return false;
            }
        }

        /// <summary>
        /// Creates an Azure File Share.
        /// </summary>
        /// <param name="fileShareName"></param>
        /// <returns></returns>
        public async Task<string> CreateFileShare(Guid contentItemId, string name, bool isPreview, bool replace)
        {
            FileShareCollection fileShareCollection = _fileService.GetFileShares();
            List<string> existingShareNames = GetExistingShareNamesForContent(contentItemId, name, isPreview);

            try
            {
                string newFileShareName = GenerateUniqueNewShareName(contentItemId, name, isPreview);

                // Generate new name first because after shareClient.DeleteIfExistsAsync an existing name is not found while delete is not completed
                if (replace)
                {
                    foreach (string existingName in existingShareNames)
                    {
                        Log.Debug($"Deleting share {existingName}");  // temporary or improve
                        await RemoveFileShareIfExists(existingName);
                    };
                }

                try
                {
                    Log.Debug($"Creating share {newFileShareName}");  // temporary or improve

                    // If the name is in use by a share currently being deleted this will throw
                    ArmOperation<FileShareResource> fileShareCreateOperation = await fileShareCollection.CreateOrUpdateAsync(WaitUntil.Completed, newFileShareName, new FileShareData());
                    await fileShareCreateOperation.WaitForCompletionAsync(); // Do assignment and return??

                    return newFileShareName;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, $"Failed to create share named {newFileShareName}");  // temporary or improve

                    FileShareResource existingShare = _fileService.GetFileShare(newFileShareName, "stats");

                    throw;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Error creating Azure file share for: contentItemId=<{contentItemId}>, name=<{name}>, isPreview=<{isPreview}>");  // temporary or improve
                throw;
            }
        }

        /// <summary>
        /// Removes a File Share.
        /// </summary>
        /// <param name="fileShareName"></param>
        /// <returns></returns>
        public async Task RemoveFileShareIfExists(string name)
        {
            try
            {
                Pageable<StorageAccountKey> storageAccountKeys = _storageAccount.GetKeys();
                FileShareResource fileShareResource = _fileService.GetFileShare(name);

                string connectionString = $"DefaultEndpointsProtocol=https;AccountName={_storageAccount.Data.Name};AccountKey={storageAccountKeys.First().Value};EndpointSuffix=core.windows.net";
                ShareClient shareClient = new ShareClient(connectionString, fileShareResource.Data.Name);
                bool response = await shareClient.DeleteIfExistsAsync(); // May take several minutes after initiated.
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error removing Azure File Share named: <{name}>");  // temporary or improve
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
        public async Task ClearFileShare(string fileShareName, string directoryName)
        {
            var storageAccountKeys = _storageAccount.GetKeys();
            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={_storageAccount.Data.Name};AccountKey={storageAccountKeys.First().Value};EndpointSuffix=core.windows.net";
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
                Log.Error(ex, "Error creating directory within Azure File Share.");  // temporary or improve
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
                Log.Error(ex, "Error uploading file to Azure File Share.");  // temporary or improve
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
                Log.Error(ex, "Error fetcing Azure subscription, resource group, or storage account info.");  // temporary or improve
                throw;
            }
        }
        #endregion
    }
}

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerRegistry.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.Data.SqlTypes;

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

        public StorageAccountInfo GetStorageAccountInfo()
        {
            AssertValid(CredentialScope.Storage);

            StorageAccountInfo info = new StorageAccountInfo(_storageAccount.Data.Name, _storageAccount.GetKeys()
                                                                                                       .Select(k => k.Value)
                                                                                                       .ToList() );
            return info;
        }


        /// <summary>
        /// Initializes this api with Azure client credentials
        /// </summary>
        /// <param name="credentials">An enumerable of credentials objects</param>
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

            List<string> existingShareNames = GetExistingShareNamesForContent(contentItemId, name, isPreview);
            Log.Debug($"Found shares with names:{string.Join("", existingShareNames.Select(n => $"{Environment.NewLine}    {n}"))}");  // temporary or improve

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
            while (existingShareNames.Contains(returnValue));

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
            IEnumerable<string> query = _fileService.GetFileShares()
                                                    .Select(s => s.Data.Name)
                                                    .Where(n => n.StartsWith($"content-{contentItemId.ToString("N")}-{name}-"));
            query = isPreview
                  ? query.Where(n => n.EndsWith("-preview"))
                  : query.Where(n => !n.EndsWith("-preview"));

            List<string> fileShareNames = query.ToList();

            return fileShareNames;
        }

        public bool FindExistingShareByName(Guid contentItemId, string name, bool isPreview, out string existingShareResource)
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
                existingShareResource = foundShare.Data.Name;
                return true;
            }
            else
            {
                existingShareResource = null;
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isPreview">If true only find preview shares named as preview, if false return shares not named as preview</param>
        /// <param name="matchingSubstringList"></param>
        /// <param name="startsWithString"></param>
        /// <returns></returns>
        public List<string> FindExistingShareBySubstringMatch(bool isPreview, List<string> matchingSubstringList = null, string startsWithString = null)
        {

            IEnumerable<string> query = _fileService.GetFileShares()
                                                    .Where(s => !(s.Data.IsDeleted.HasValue && s.Data.IsDeleted.Value)) // Don't return a share that is being deleted
                                                    .Select(fs => fs.Data.Name);
            
            if (!string.IsNullOrWhiteSpace(startsWithString))
            {
                query = query.Where(s => s.StartsWith(startsWithString));
            }

            if (matchingSubstringList != null)
            {
                foreach (string substring in matchingSubstringList)
                {
                    query = query.Where(s => s.Contains(substring));
                }
            }

            query = query.Where(s => isPreview == s.EndsWith("-preview"));

            return query.Select(s => s).ToList();
        }

        /// <summary>
        /// Creates an Azure File Share.
        /// </summary>
        /// <param name="fileShareName"></param>
        /// <returns></returns>
        public async Task<string> CreateFileShare(Guid contentItemId, string shortName, bool isPreview, bool replace)
        {
            FileShareCollection fileShareCollection = _fileService.GetFileShares();
            List<string> existingShareNames = GetExistingShareNamesForContent(contentItemId, shortName, isPreview);

            try
            {
                string newFileShareName = GenerateUniqueNewShareName(contentItemId, shortName, isPreview);

                // Generate new name first because after shareClient.DeleteIfExistsAsync an existing name is not found while delete is not completed
                if (replace)
                {
                    foreach (string existingName in existingShareNames)
                    {
                        Log.Debug($"Deleting share {existingName}");  // temporary or improve
                        await RemoveFileShareIfExistsAsync(existingName);
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
                Log.Warning(ex, $"Error creating Azure file share for: contentItemId=<{contentItemId}>, shortName=<{shortName}>, isPreview=<{isPreview}>");  // temporary or improve
                throw;
            }
        }

        public async Task ExtractCompressedFileToShare(string fileFullPath, string shareName, bool overwriteExistingFiles)
        {
            if (!File.Exists(fileFullPath))
            {
                throw new FileNotFoundException("Unable to extract archive, not found", fileFullPath);
            }

            if (fileFullPath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
            {
                using (Stream zipFileStream = File.OpenRead(fileFullPath))
                {
                    var zipArchive = new ZipArchive(zipFileStream);
                    foreach (var entry in zipArchive.Entries)
                    {
                        FileAttributes entryAttributes = (FileAttributes)entry.ExternalAttributes;
                        if ((entryAttributes & FileAttributes.Directory) != FileAttributes.Directory)  // only for a file
                        {
                            string tempFileName = Path.Combine(Path.GetDirectoryName(fileFullPath), Guid.NewGuid().ToString());
                            try
                            {
                                entry.ExtractToFile(tempFileName);
                                await UploadFileToShare(tempFileName, shareName, entry.FullName, overwriteExistingFiles);
                            }
                            finally
                            {
                                File.Delete(tempFileName);
                            }
                        }
                    }
                }
            }
            else
            {
                // TODO uh oh
            }
        }

        public async Task DownloadAndCompressShareContents(Guid contentItemId, string userShareName, string localDownloadPath, string zipPath)
        {
            List<string> possibleShareNames = GetExistingShareNamesForContent(contentItemId, userShareName, false);
            string targetedShareName = possibleShareNames.SingleOrDefault();

            #region Ensure temporary directory is created
            try
            {
                Directory.CreateDirectory(localDownloadPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error downloading compressed Azure File Share contents: local directory {localDownloadPath} could not be created.");
                throw;
            }
            #endregion

            Pageable<StorageAccountKey> storageAccountKeys = _storageAccount.GetKeys();
            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={_storageAccount.Data.Name};AccountKey={storageAccountKeys.First().Value};EndpointSuffix=core.windows.net";
            ShareServiceClient shareServiceClient = new ShareServiceClient(connectionString);
            ShareClient shareClient = shareServiceClient.GetShareClient(targetedShareName);
            await DownloadDirectoryRecursiveAsync(shareClient, shareClient.GetRootDirectoryClient(), localDownloadPath, targetedShareName);

            #region Compression and clean-up
            try
            {
                try
                {
                    ZipFile.CreateFromDirectory(localDownloadPath, zipPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error compressing contents of Azure File share {targetedShareName}, compressed from {localDownloadPath} to location {zipPath}");
                    throw;
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(localDownloadPath, true);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error removing the temporary local directory {localDownloadPath} after compressing the Azure File Share contents.");
                    throw;
                }
            }
            #endregion
        }

        private async Task DownloadDirectoryRecursiveAsync(ShareClient shareClient, ShareDirectoryClient shareDirectoryClient, string localDownloadPath, string shareName)
        {
            try
            {
                await foreach (ShareFileItem nextFileItem in shareDirectoryClient.GetFilesAndDirectoriesAsync())
                {
                    string nextItemPath = Path.Combine(localDownloadPath, nextFileItem.Name);
                    if (nextFileItem.IsDirectory)
                    {
                        Directory.CreateDirectory(nextItemPath);
                        ShareDirectoryClient subDirectoryClient = shareClient.GetDirectoryClient(nextFileItem.Name);
                        await DownloadDirectoryRecursiveAsync(shareClient, subDirectoryClient, nextItemPath, shareName);
                    }
                    else
                    {
                        ShareFileClient fileClient = shareDirectoryClient.GetFileClient(nextFileItem.Name);
                        Response<ShareFileDownloadInfo> downloadResponse = await fileClient.DownloadAsync();
                        using FileStream localFileStream = File.Create(nextItemPath);
                        await downloadResponse.Value.Content.CopyToAsync(localFileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error downloading contents of Azure File share {shareName} to local location {localDownloadPath}");
                return;
            }
        }

        /// <summary>
        /// Removes a File Share.
        /// </summary>
        /// <param name="fileShareName"></param>
        /// <returns></returns>
        public async Task RemoveFileShareIfExistsAsync(string name)
        {
            try
            {
                Pageable<StorageAccountKey> storageAccountKeys = _storageAccount.GetKeys();
                try
                {
                    FileShareResource fileShareResource = _fileService.GetFileShare(name);
                    string connectionString = $"DefaultEndpointsProtocol=https;AccountName={_storageAccount.Data.Name};AccountKey={storageAccountKeys.First().Value};EndpointSuffix=core.windows.net";
                    ShareClient shareClient = new ShareClient(connectionString, fileShareResource.Data.Name);
                    bool response = await shareClient.DeleteIfExistsAsync(); // May take several minutes after initiated.
                }
                catch (Azure.RequestFailedException ex) when (ex.HResult == -2146233088)  // GetFileShare throws this when the share is not found
                {
                    return;
                }

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
        public void ClearFileShareDirectory(string fileShareName, bool recursive, string directoryName = "/")
        {
            var storageAccountKeys = _storageAccount.GetKeys();
            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={_storageAccount.Data.Name};AccountKey={storageAccountKeys.First().Value};EndpointSuffix=core.windows.net";
            ShareClient shareClient = new ShareClient(connectionString, fileShareName);
            ShareDirectoryClient startDirectoryClient = shareClient.GetDirectoryClient(directoryName);

            foreach (ShareFileItem f in startDirectoryClient.GetFilesAndDirectories())
            {
                if (recursive && f.IsDirectory) 
                {
                    ClearFileShareDirectory(fileShareName, recursive, Path.Combine(directoryName, f.Name));
                    ShareDirectoryClient subClient = startDirectoryClient.GetSubdirectoryClient(f.Name);
                    subClient.DeleteIfExists();
                }
                else
                {
                    ShareFileClient fileClient = new ShareFileClient(new Uri(startDirectoryClient.Uri, f.Name));
                    fileClient.DeleteIfExists();
                }
            }
            
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
        public async Task CreateFileShareDirectory(string fileShareName, string directoryName)
        {
            try
            {
                var storageAccountKeys = _storageAccount.GetKeys();
                string connectionString = $"DefaultEndpointsProtocol=https;AccountName={_storageAccount.Data.Name};AccountKey={storageAccountKeys.FirstOrDefault().Value};EndpointSuffix=core.windows.net";
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

        public async Task<bool> UploadFileToShare(string fileName, string fileShareName, string destinationFileFullPath, bool overwriteExistingFiles)
        {
            try
            {
                Pageable<StorageAccountKey> storageAccountKeys = _storageAccount.GetKeys();
                string connectionString = $"DefaultEndpointsProtocol=https;AccountName={_storageAccount.Data.Name};AccountKey={storageAccountKeys.FirstOrDefault().Value};EndpointSuffix=core.windows.net";

                ShareClient shareClient = new ShareClient(connectionString, fileShareName);
                ShareDirectoryClient directoryClient= shareClient.GetRootDirectoryClient();

                string destinationFolder = Path.IsPathRooted(destinationFileFullPath)
                    ? Path.GetDirectoryName(destinationFileFullPath)
                    : Path.Combine("/", Path.GetDirectoryName(destinationFileFullPath));
                string destinationFileName = Path.GetFileName(destinationFileFullPath);

                if (destinationFolder != "/")
                {
                    directoryClient = directoryClient.GetSubdirectoryClient(destinationFolder);
                    await directoryClient.CreateIfNotExistsAsync();
                }

                ShareFileClient targetFileClient = directoryClient.GetFileClient(destinationFileName);
                if (targetFileClient.Exists() && !overwriteExistingFiles)
                {
                    return false;
                }

                FileInfo fileInfo = new FileInfo(fileName);
                targetFileClient.Create(fileInfo.Length);

                if (fileInfo.Length > 0)
                {
                    using (FileStream sourceFileStream = File.OpenRead(fileName))
                    {
                        targetFileClient.Upload(sourceFileStream);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error uploading file to Azure File Share from provided stream.");  // temporary or improve
                throw;
            }
        }

        /// <summary>
        /// Does not close the source stream
        /// </summary>
        /// <param name="source"></param>
        /// <param name="length"></param>
        /// <param name="fileShareName"></param>
        /// <param name="destinationFileFullPath"></param>
        /// <returns></returns>
        public async Task UploadStreamToShare(Stream source, long length, string fileShareName, string destinationFileFullPath)
        {
            Pageable<StorageAccountKey> storageAccountKeys = _storageAccount.GetKeys();
            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={_storageAccount.Data.Name};AccountKey={storageAccountKeys.FirstOrDefault().Value};EndpointSuffix=core.windows.net";

            ShareClient shareClient = new ShareClient(connectionString, fileShareName);
            ShareDirectoryClient directoryClient = shareClient.GetRootDirectoryClient();

            string destinationFolder = Path.IsPathRooted(destinationFileFullPath)
                ? Path.GetDirectoryName(destinationFileFullPath)
                : Path.Combine("/", Path.GetDirectoryName(destinationFileFullPath));
            string destinationFileName = Path.GetFileName(destinationFileFullPath);

            if (destinationFolder != "/")
            {
                directoryClient = directoryClient.GetSubdirectoryClient(destinationFolder);
                await directoryClient.CreateIfNotExistsAsync();
            }

            ShareFileClient targetFileClient = directoryClient.GetFileClient(destinationFileName);
            targetFileClient.Create(length);

            targetFileClient.Upload(source);
        }

        public void DuplicateShareContents(string sourceShareName, string destinationShareName)
        {
            Pageable<StorageAccountKey> storageAccountKeys = _storageAccount.GetKeys();
            string connectionString = $"DefaultEndpointsProtocol=https;AccountName={_storageAccount.Data.Name};AccountKey={storageAccountKeys.First().Value};EndpointSuffix=core.windows.net";

            FileShareResource sourceFileShareResource = _fileService.GetFileShare(sourceShareName);
            ShareClient sourceShareClient = new ShareClient(connectionString, sourceFileShareResource.Data.Name);

            FileShareResource destinationFileShareResource = _fileService.GetFileShare(destinationShareName);
            ShareClient destinationShareClient = new ShareClient(connectionString, destinationFileShareResource.Data.Name);

            DuplicateDirectoryContents(sourceShareClient.GetRootDirectoryClient(), destinationShareClient.GetRootDirectoryClient());
        }

        private void DuplicateDirectoryContents(ShareDirectoryClient sourceDirectoryClient, ShareDirectoryClient destinationDirectoryClient)
        {
            Pageable<ShareFileItem> items = sourceDirectoryClient.GetFilesAndDirectories();

            // First, copy files in this directory
            foreach (ShareFileItem item in items.Where(i => !i.IsDirectory))
            {
                ShareFileClient sourceFileClient = sourceDirectoryClient.GetFileClient(item.Name);
                using (Stream sourceStream = sourceFileClient.OpenRead())
                {
                    ShareFileClient destinationFileClient = destinationDirectoryClient.GetFileClient(item.Name);
                    destinationFileClient.Create(item.FileSize.Value);
                    destinationFileClient.Upload(sourceStream);
                }
            }

            // Second, recurse subfolders
            foreach (ShareFileItem item in items.Where(i => i.IsDirectory))
            {
                ShareDirectoryClient sourceSubDirectoryClient = sourceDirectoryClient.GetSubdirectoryClient(item.Name);
                ShareDirectoryClient destinationSubDirectoryClient = destinationDirectoryClient.GetSubdirectoryClient(item.Name);
                destinationSubDirectoryClient.CreateIfNotExists();
                DuplicateDirectoryContents(sourceSubDirectoryClient, destinationSubDirectoryClient);
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

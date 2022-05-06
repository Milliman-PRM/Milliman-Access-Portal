using ContainerizedAppLib.AzureRestApiModels;
using Flurl.Http;
using MapCommonLib;
using MapCommonLib.ContentTypeSpecific;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ContainerizedAppLib
{
    public class ContainerizedAppLibApi : ContentTypeSpecificApiBase
    {
        public ContainerizedAppLibApiConfig Config { get; private set; }
        private string _acrToken, _aciToken, _repositoryName;

        /// <summary>
        /// Gets the URI for a Container Content item.
        /// </summary>
        /// <param name="typeSpecificContentIdentifier">The identifier for the Content Item.</param>
        /// <param name="UserName">The user accessing the Content Uri.</param>
        /// <param name="thisHttpRequest">The request being made.</param>
        /// <returns>The Content URI as a UriBuilder object.</returns>
        public async override Task<UriBuilder> GetContentUri(string typeSpecificContentIdentifier, string UserName, HttpRequest thisHttpRequest)
        {
            await Task.Yield();

            string[] QueryStringItems = new string[]
            {
                $"group={typeSpecificContentIdentifier}",
            };

            UriBuilder contentUri = new UriBuilder
            {
                Scheme = thisHttpRequest.Scheme,
                Host = thisHttpRequest.Host.Host ?? "localhost",  // localhost is probably error in production but won't crash
                Port = thisHttpRequest.Host.Port ?? -1,
                Path = $"/AuthorizedContent/ContainerizedApp",
                Query = string.Join("&", QueryStringItems),
            };

            return contentUri;
        }


        public ContainerizedAppLibApi(ContainerizedAppLibApiConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Asynchronous initializer, chainable with the constructor.
        /// Generates secure tokens for communicating both with Azure Container Registry
        /// and Azure Container Instances.
        /// </summary>
        /// <returns>this</returns>
        public async Task<ContainerizedAppLibApi> InitializeAsync(string repositoryName)
        {
            _repositoryName = repositoryName;

            try
            {
                await GetAcrAccessTokenAsync();
                await GetAciAccessTokenAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obtaining ContainerizedAppLibApi authentication tokens.");
                throw;
            }

            return this;
        }

        /// <summary>
        /// Initialize a new access token for communicating with the Azure Container Registry.
        /// </summary>
        private async Task GetAcrAccessTokenAsync()
        {
            string tokenEndpointWithScope = $"{Config.ContainerRegistryTokenEndpoint}&scope=repository:{_repositoryName}:pull,push,delete";
            try
            {
                ACRAuthenticationResponse response = await StaticUtil.DoRetryAsyncOperationWithReturn<Exception, ACRAuthenticationResponse>(async () =>
                                                        await tokenEndpointWithScope
                                                            .WithHeader("Authorization", $"Basic {Config.ContainerRegistryCredentialBase64}")
                                                            .GetAsync()
                                                            .ReceiveJson<ACRAuthenticationResponse>(), 3, 100);

                _acrToken = response.AccessToken;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception from ContainerizedAppLibApi.GetAcrAccessTokenException: Azure Container Registry access token could not be fetched.");
            }
        }

        #region Container Registry

        /// <summary>
        /// Communicates with the Azure Container Registry to fetch the manifest for
        /// the current repository.
        /// </summary>
        /// <param name="tag">
        /// Specifies the tag of the manifest trying to be retrieved. Since repositories
        /// can have multiple manifests, each with their own tags, this parameter ensures
        /// that only the manifest that owns the parameter tag will be retrieved.
        /// </param>
        /// <returns>dynamic response object of manifest</returns>
        public async Task<object> GetRepositoryManifest(string tag = "latest")
        {
            string manifestEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{_repositoryName}/manifests/{tag}";
            try
            {
                dynamic response = await manifestEndpoint
                                        .WithHeader("Authorization", $"Bearer {_acrToken}")
                                        .WithHeader("Accept", "application/vnd.docker.distribution.manifest.v2+json")
                                        .GetJsonAsync();
                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception from ContainerizedAppLibApi.GetRepositoryManifest: Could not fetch repository manifest.");
                throw;
            }
        }
        
        /// <summary>
        /// Retrieves a list of all tags for the current repository.
        /// </summary>
        /// <returns>A collection of all tags for the current repositories, represented as a list of strings</returns>
        public async Task<List<string>> GetRepositoryTags()
        {
            string manifestEndpoint = $"https://{Config.ContainerRegistryUrl}/v1/{_repositoryName}/_tags";
            
            try
            {
                dynamic response = await manifestEndpoint
                                        .WithHeader("Authorization", $"Bearer {_acrToken}")
                                        .WithHeader("Accept", "application/vnd.docker.distribution.manifest.v2+json")
                                        .GetJsonAsync();
                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception from ContainerizedAppLibApi.GetRepositoryTags: Could not fetch repository tags.");
                throw;
            }
        }

        /// <summary>
        /// Removes a manifest belonging to the current repository.
        /// </summary>
        /// <param name="digest">The identifying digest for the manifest to be removed.</param>
        /// <returns></returns>
        private async Task DeleteRepositoryManifest(string digest)
        {
            string deleteImageManifestEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{_repositoryName}/manifests/{digest}";
            try
            {
                await deleteImageManifestEndpoint
                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Exception from ContainerizedAppLibApi.DeleteRepositoryManifest: Failed to delete image manifest for {_repositoryName} with digest {digest}.");
                throw;
            }
        }

        /// <summary>
        /// Deletes the current repository.
        /// This is different from removal of an individual manifest or tag, as it will
        /// remove all resources belonging to the current repository, including all tagged
        /// images and their corresponding manifests.
        /// </summary>
        /// <returns></returns>
        public async Task DeleteRepository()
        {
            string deleteRepositoryEndpoint = $"https://{Config.ContainerRegistryUrl}/acr/v1/{_repositoryName}";
            try
            {
                await deleteRepositoryEndpoint
                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception from ContainerizedAppLibApi.DeleteRepositoryManifest: Failed to delete repository {_repositoryName}.");
                throw;
            }
        }

        /// <summary>
        /// Removes an individual tag from the current repository.
        /// </summary>
        /// <param name="tag">The tag to be removed.</param>
        /// <returns></returns>
        public async Task DeleteTag(string tag)
        {
            string deleteTagEndpoint = $"https://{Config.ContainerRegistryUrl}/acr/v1/{_repositoryName}/_tags/{tag}";
            try
            {
                await deleteTagEndpoint
                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception from ContainerizedAppLibApi.DeleteTag: Failed to delete image tag for {_repositoryName}:{tag}");
                throw;
            }
        }

        /// <summary>
        /// Pushes an image manifest to the current repository, and assigns it a tag in the
        /// repository.
        /// </summary>
        /// <param name="manifestContents">The manifest of the image, stringified.</param>
        /// <param name="tag">The tag to assign the manifest to.</param>
        /// <returns></returns>
        public async Task PushImageManifest(string manifestContents, string tag)
        {
            string manifestUploadEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{_repositoryName}/manifests/{tag}";

            try
            {
                IFlurlResponse manifestUploadResponse = await manifestUploadEndpoint
                                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                                    .WithHeader("Content-Type", "application/vnd.docker.distribution.manifest.v2+json")
                                    .PutStringAsync(manifestContents);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception from ContainerizedAppLibApi.PushImageManifest: Failed to push manifest to {_repositoryName}:{tag}.");
                throw;
            }
        }
        
        /// <summary>
        /// Pushes an image to the Azure Container Registry.
        /// 
        /// There are 3 steps to this process in totally.
        /// 1. Compile a list of all layers needed for the image to be assembled in ACR.
        /// 2. For each image layer, check to see if it already exists in the current repository.
        ///     a. If the image layer (blob) exists in the repository, do not re-upload.
        ///     b. If the image layer (blob) does exist in the repository, continue.
        /// 3. For each image layer (blob), upload the layer to ACR.
        ///     - See ContainerizedAppLibApi.UploadBlob(digest, path)
        /// 4. Upload image manifest, with specified tag.
        /// </summary>
        /// <param name="imageFileFullPath">The full path location of the image layers and manifest.</param>
        /// <param name="tag">The tag to assign to this image in the current repository.</param>
        /// <returns></returns>
        public async Task PushImageToRegistry(string imageFileFullPath, string tag = "latest")
        {
#warning TODO note in publishing user guide that the tar file should use only ASCII encoding in the name fields

            string workingFolderName = Path.GetDirectoryName(imageFileFullPath);
            GlobalFunctions.ExtractFromTar(imageFileFullPath);

            try
            {
                #region Compile layers
                string manifestPath = Path.Combine(workingFolderName, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    throw new ApplicationException($"Invalid image file: Manifest {manifestPath} cannot be found.");
                }

                string manifestContents = File.ReadAllText(manifestPath).Trim('[', ']');
                JObject manifestObj = JObject.Parse(manifestContents);

                List<BlobData> layerData = manifestObj.SelectToken("layers").ToObject<List<BlobData>>();
                BlobData configObject = manifestObj.SelectToken("config").ToObject<BlobData>();
                List<string> blobDigests = layerData.Select(layerData => layerData.Digest.Replace("sha256:", ""))
                                                    .Append(configObject.Digest.Replace("sha256:", ""))  // Include config BLOB to create a new repository.
                                                    .ToList();
                #endregion

                foreach (string blobDigest in blobDigests)
                {
                    if (!await BlobDoesExist($"sha256:{blobDigest}"))
                    {
                        string blobPath = Path.Combine(workingFolderName, blobDigest);
                        await UploadBlob(blobDigest, blobPath);
                    }
                }

                await PushImageManifest(manifestContents, tag);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception from ContainerizedAppLibApi.PushImageToRegistry: Failed to push image to {_repositoryName}:{tag}");
                throw;
            }
        }

        /// <summary>
        /// Determines if the specified image layer (blob) exists in the current repository.
        /// </summary>
        /// <param name="blobDigest">The digest of the blob.</param>
        /// <returns></returns>
        private async Task<bool> BlobDoesExist(string blobDigest)
        {
            string checkExistenceEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{_repositoryName}/blobs/{blobDigest}";

            try
            {
                IFlurlResponse response = await checkExistenceEndpoint
                                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                                    .AllowHttpStatus(System.Net.HttpStatusCode.NotFound) // 404 if layer is simply not found, do not throw.
                                    .HeadAsync();

                response.Headers.TryGetFirst("Docker-Content-Digest", out string responseDigest);
                return response.StatusCode == 202 && responseDigest.Equals(blobDigest, StringComparison.InvariantCultureIgnoreCase);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception in ContainerizedAppLibApi.BlobDoesExist: Error when checking existince of blob with digest {blobDigest}.");
                throw;
            }
        }

        /// <summary>
        /// Uploads an image layer (blob) to the current repository.
        /// 
        /// This occurs in 3 steps.
        /// 1. Start blob upload process.
        /// 2. Upload blob in chunks, until full blob has been uploaded.
        /// 3. End blob upload process.
        /// </summary>
        /// <param name="blobDigest">The digest of the layer image (blob).</param>
        /// <param name="pathToLayer">The full path to the layer image (blob).</param>
        /// <returns></returns>
        private async Task UploadBlob(string blobDigest, string pathToLayer)
        {
            string nextUploadLocation = "";
            IFlurlRequest startBlobUploadEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{_repositoryName}/blobs/uploads/"
                .WithHeader("Authorization", $"Bearer {_acrToken}");

            try
            {
                #region Start blob upload.
                IFlurlResponse response = await startBlobUploadEndpoint
                    .WithHeader("Access-Control-Expose-Headers", "Docker-Content-Digest")
                    .WithHeader("Accept", "application/vnd.docker.distribution.manifest.v2+json ")
                    .PostAsync();
                response.Headers.TryGetFirst("Location", out nextUploadLocation);
                #endregion

                if (response.StatusCode == 202 && !string.IsNullOrEmpty(nextUploadLocation))
                {
                    #region Upload blob.
                    if (File.Exists(pathToLayer))
                    {
                        using (FileStream fileStream = new FileStream(pathToLayer, FileMode.Open, FileAccess.Read))
                        {
                            // Do a hash check on the BLOB to ensure that upload of layer data occurs in an OCI compliant way.
                            using (SHA256 hasher = SHA256.Create())
                            {
                                StringBuilder builder = new StringBuilder();
                                byte[] result = hasher.ComputeHash(fileStream);
                                foreach (byte b in result)
                                {
                                    builder.Append(b.ToString("x2"));
                                }
                                if (!builder.ToString().Equals(blobDigest))
                                {
                                    throw new Exception($"Error on pushing image: Calculated SHA256 hash does not match given layer digest.");
                                }
                            }

                            fileStream.Seek(0, SeekOrigin.Begin); // Reset stream position since position gets moved when hash is calculated.

                            long totalNumberOfBytesToRead = fileStream.Length;
                            int totalNumberOfBytesRead = 0;
                            int defaultChunkSize = 10_485_760; // Maximum 10 MB chunk uploads.
                            byte[] rawFileBytes = new byte[totalNumberOfBytesToRead];
                            while (totalNumberOfBytesToRead > 0)
                            {
                                int chunkSize = Math.Min(defaultChunkSize, (int)(totalNumberOfBytesToRead));
                                int numberOfBytesRead = fileStream.Read(rawFileBytes, totalNumberOfBytesRead, chunkSize);
                                byte[] chunkBytes = new byte[chunkSize];
                                Array.Copy(rawFileBytes, totalNumberOfBytesRead, chunkBytes, 0, chunkSize);

                                string blobUploadEndpoint = $"https://{Config.ContainerRegistryUrl}{nextUploadLocation}";
                                string base64String = Convert.ToBase64String(rawFileBytes);
                                IFlurlResponse blobUploadResponse = await blobUploadEndpoint
                                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                                    .WithHeader("Accept", "application/vnd.oci.image.manifest.v2+json")
                                    .WithHeader("Accept", "application/vnd.docker.distribution.manifest.v2+json")
                                    .WithHeader("Access-Control-Expose-Headers", "Docker-Content-Digest")
                                    .WithHeader("Content-Length", chunkSize)
                                    .WithHeader("Content-Range", $"{totalNumberOfBytesRead}-{totalNumberOfBytesRead + chunkSize - 1}")
                                    .WithHeader("Content-Type", "application/octet-stream")
                                    .PatchAsync(new ByteArrayContent(chunkBytes));
                                blobUploadResponse.Headers.TryGetFirst("Location", out nextUploadLocation);

                                totalNumberOfBytesRead += chunkSize;
                                totalNumberOfBytesToRead -= chunkSize;
                            }
                        }
                    }
                    #endregion

                    #region Finish blob upload.
                    string finishBlobUploadEndpoint = $"https://{Config.ContainerRegistryUrl}{nextUploadLocation}&digest=sha256:{blobDigest}";
                    IFlurlResponse endUploadResponse = await finishBlobUploadEndpoint
                                                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                                                    .PutAsync();
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception from ContainerizedAppLibApi.UploadBlob: Error attempting to upload blob with digest {blobDigest} to repository {_repositoryName}.");
                throw;
            }
        }

        /// <summary>
        /// Finds the image in the repository with a given tag and creates a copy with
        /// a new tag.
        /// </summary>
        /// <param name="oldTag">Tagged image to copy and re-tag</param>
        /// <param name="newTag">Tag to use and add to new image</param>
        /// <param name="deleteOldTag">Optional: determines if old image is to be removed from current repository</param>
        /// <returns></returns>
        public async Task RetagImage(string oldTag, string newTag, bool deleteOldTag = true)
        {
            // Get existing manifest.
            object manifestObj = await GetRepositoryManifest(oldTag);
            string parsedManifestString = JsonConvert.SerializeObject(manifestObj, Formatting.None);

            // Push same manifest with new tag.
            await PushImageManifest(parsedManifestString, newTag);

            // Remove previously tagged image.
            if (deleteOldTag)
            {
                await DeleteTag(oldTag);
            }
        }
        #endregion

        #region Container Instances

        /// <summary>
        /// Initialize a new access token for communicating with the Azure Container Instances API.
        /// </summary>
        /// <returns></returns>
        public async Task GetAciAccessTokenAsync()
        {
            try
            {
                MicrosoftAuthenticationResponse response = await StaticUtil.DoRetryAsyncOperationWithReturn<Exception, MicrosoftAuthenticationResponse>(async () =>
                                                                        await Config.ContainerInstanceTokenEndpoint
                                                                        .PostMultipartAsync(mp => mp
                                                                            .AddString("grant_type", Config.AciGrantType)
                                                                            .AddString("scope", "https://management.azure.com/.default")
                                                                            .AddString("client_id", Config.AciClientId)
                                                                            .AddString("client_secret", Config.AciClientSecret)
                                                                        )
                                                                        .ReceiveJson<MicrosoftAuthenticationResponse>(), 3, 100);

                if (response.ExpiresIn > 0 && response.ExtExpiresIn > 0)
                {
                    _aciToken = response.AccessToken;
                }
                else
                {
                    Log.Warning("Invalid response when authenticating to Azure Container Instances, response object is {@response}", response);
                    response = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception from ContainerizedAppLibApi.GetAciAccessTokenAsync: Azure Container Instances access token could not be fetched");
            }
        }

        /// <summary>
        /// Creates a new Container Group, and then polls the Container Group creation process
        /// for progress and delivers the accessible IP address and port once the Container
        /// Group is successfully running.
        /// </summary>
        /// <param name="containerGroupName">Name to assign to Container Group.</param>
        /// <param name="containerImageName">The image to use in the Container.</param>
        /// <param name="containerImageTag">The image tag to use in the Container.</param>
        /// <param name="ipType">The type of IP Address (public or private).</param>
        /// <param name="cpuCoreCount">The number of CPU cores for the Container Group.</param>
        /// <param name="memorySizeInGB">The amount of RAM in GB for the Container Group.</param>
        /// <param name="resourceTags">The Container Group resource tags.</param>
        /// <param name="vnetId">The virtual network ID.</param>
        /// <param name="vnetName">The name of the virtual network.</param>
        /// <param name="containerPorts">A list of ports to be exposed on the Container Group.</param>
        /// <returns></returns>
        public async Task<string> RunContainer(string containerGroupName, 
                                               string containerImageName, 
                                               string containerImageTag, 
                                               string ipType, 
                                               int cpuCoreCount, 
                                               double memorySizeInGB, 
                                               ContainerGroupResourceTags resourceTags,
                                               string vnetId, 
                                               string vnetName,
                                               params ushort[] containerPorts)
        {
            try
            {
                string imagePath = $"{Config.ContainerRegistryUrl}/{containerImageName}:{containerImageTag}";
           
                bool createResult = await CreateContainerGroup(containerGroupName, imagePath, ipType, cpuCoreCount, memorySizeInGB, resourceTags, vnetId, vnetName, containerPorts);

                if (!createResult)
                {
                    Log.Error("Failed to create container group");
                    return null;
                }

                DateTime timeLimit = DateTime.UtcNow + TimeSpan.FromMinutes(5);
                string containerGroupProvisioningState = null;
                string containerGroupIpAddress = null;
                ushort applicationPort = 0;
                string containerGroupInstanceViewState = null;
                ContainerGroup_GetResponseModel containerGroupModel = default;

                while (DateTime.UtcNow < timeLimit && new[] { null, "Pending", "Creating" }.Contains(containerGroupProvisioningState))
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    containerGroupModel = await GetContainerGroupDetails(containerGroupName);

                    containerGroupProvisioningState = containerGroupModel.Properties.ProvisioningState;
                    containerGroupIpAddress = containerGroupModel.Properties.IpAddress.Ip;
                    containerGroupInstanceViewState = containerGroupModel.Properties.InstanceView.State;

                    try
                    {
                        applicationPort = containerGroupModel.Properties.IpAddress.Ports.Single().Port;
                    }
                    catch { }

                    Log.Information($"ContainerGroup provisioning state {containerGroupProvisioningState}, " +
                                    $"instanceView state {containerGroupModel.Properties?.InstanceView?.State}, " +
                                    $"IP {containerGroupModel.Properties.IpAddress.Ip}, " +
                                    $"containers states: <{string.Join(",", containerGroupModel.Properties.Containers.Select(c => c.Properties.Instance_View?.CurrentState?.State))}>");
                }

                Log.Information($"Container group full response: {{@model}}", containerGroupModel);

                #region This region waits until the application in the container has launched/initialized.  How much time is enough, different applications have different initializations

                // This loop waits until the IP:port is listening for TCP connection
                // for (Stopwatch stopWatch = Stopwatch.StartNew(); stopWatch.Elapsed < TimeSpan.FromSeconds(60); await Task.Delay(TimeSpan.FromSeconds(1))
                //{
                //    try
                //    {
                //        TcpClient tcpClient = new TcpClient(containerGroupIpAddress, applicationPort);
                //        Log.Information($"socket connected state is {tcpClient.Connected}");
                //        tcpClient.Close();
                //    }
                //    catch (Exception ex)
                //    {
                //        Log.Information(ex, $"socket connection exception");
                //        continue;
                //    }
                //    break;
                //}

                // This block waits until a search of the container log finds predetermined text, which should be obtained from a type specific info property of the content
                string containerLogMatchString = string.Empty;
                containerLogMatchString = "Listening on http";  // works for for Shiny
                if (!string.IsNullOrEmpty(containerLogMatchString))
                {
                    string log = string.Empty;
                    for (Stopwatch logTimer = new Stopwatch(); 
                         logTimer.Elapsed < TimeSpan.FromSeconds(60) && !log.Contains(containerLogMatchString, StringComparison.InvariantCultureIgnoreCase); 
                         await Task.Delay(TimeSpan.FromSeconds(1)))
                    {
                        try
                        {
                            log = await GetContainerLogs(containerGroupName, containerGroupName);
                        }
                        catch { }

                        Log.Debug($"Container logs: {log}");
                    }
                }
                #endregion

                return $"http://{containerGroupIpAddress}:{applicationPort}";
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception in ContainerizedAppLibApi.RunContainer: Error trying to run Container Group {containerGroupName} with image {containerImageName}:{containerImageTag}.");
                throw;
            }

            return "";
        }

        /// <summary>
        /// Creates a new Container Group and starts it.
        /// </summary>
        /// <param name="containerGroupName">The name of the Container Group.</param>
        /// <param name="containerImageName">The name of the ACR image to be used in the Container Group.</param>
        /// <param name="ipType">The type of IP Address used (public or private).</param>
        /// <param name="cpuCoreCount">The number of CPU cores for the Container Group.</param>
        /// <param name="memorySizeInGB">The amount of RAM in GB for the Container Group.</param>
        /// <param name="resourceTags">The Container Group resource tags.</param>
        /// <param name="vnetId">The ID of the virtual network being used.</param>
        /// <param name="vnetName">The name of the virtual network being used.</param>
        /// <param name="containerPorts">A list of ports to be exposed on the Container Group.</param>
        /// <returns>A bool representing whether or not creation responded with a 201 success code.</returns>
        /// <exception cref="ApplicationException"></exception>
        private async Task<bool> CreateContainerGroup(string containerGroupName, string containerImageName, string ipType, int cpuCoreCount, double memorySizeInGB, ContainerGroupResourceTags resourceTags, string vnetId = null, string vnetName = null, params ushort[] containerPorts)
        {
            string createContainerGroupEndpoint = $"https://management.azure.com/subscriptions/{Config.AciSubscriptionId}/resourceGroups/{Config.AciResourceGroupName}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}?api-version={Config.AciApiVersion}";

            try
            {
                List<ContainerPort> containerPortObjects = containerPorts.Select(p => new ContainerPort() { Port = p }).ToList();
                ContainerGroupRequestModel requestModel = new ContainerGroupRequestModel()
                {
                    Location = "eastus", // TODO: query the location from the ResourceGroup being used to create this group
                    Properties = new ContainerGroupProperties()
                    {
                        OsType = OsTypeEnum.Linux,
                        Containers = new List<Container>()
                        {
                            new Container()
                            {
                                Name = containerGroupName,
                                Properties = new ContainerProperties()
                                {
                                    Commands = new List<string>(),
                                    Image = containerImageName,
                                    Ports = containerPortObjects,
                                    Resources = new ResourceRequirements()
                                    {
                                        ResourceRequests = new ResourceDescriptor()
                                        {
                                            CpuLimit = cpuCoreCount,
                                            MemoryInGB = memorySizeInGB,
                                        }
                                    }
                                }
                            }
                        },
                        ImageRegistryCredentials = new List<ImageRegistryCredential>()
                        {
                            new ImageRegistryCredential()
                            {
                                Username = Config.ContainerRegistryUsername,
                                Password = Config.ContainerRegistryPassword,
                                Server = Config.ContainerRegistryUrl,
                            }
                        },
                        IpAddress = new IpAddress()
                        {
                            Ports = containerPortObjects,
                            Type = ipType,
                        },
                        SubnetIds = string.IsNullOrEmpty(vnetId) || string.IsNullOrEmpty(vnetName)
                        ? null
                        : new List<ContainerGroupSubnetId>
                        {
                            new ContainerGroupSubnetId
                            {
                                Id = vnetId,
                                Name = vnetName,
                            }
                        }
                    },
                    Tags = resourceTags
                };

                string serializedRequestModel = JsonConvert.SerializeObject(requestModel, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                IFlurlResponse response = await createContainerGroupEndpoint
                                .WithHeader("Authorization", $"Bearer {_aciToken}")
                                .WithHeader("Content-Type", "application/json")
                                .PutStringAsync(serializedRequestModel);

                var x = response.GetStringAsync();

                return response.StatusCode == 201;
            }
            catch (FlurlHttpException ex)
            {
                dynamic result = await ex.GetResponseJsonAsync();
                Dictionary<string, object> error = ((IDictionary<string, object>)result.error).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                string errorMessage = $"Exception from ContainerizedAppLibApi.CreateContainerGroup: Error launching a new Container Group. Error(s):{Environment.NewLine}\t" +
                                      string.Join($"{Environment.NewLine}\t", error.Select(kvp => $"{kvp.Key}:{kvp.Value}"));

                Log.Error(ex, errorMessage);
                throw new ApplicationException(errorMessage);
            }
        }

        /// <summary>
        /// Gets information pertaining to a previously-created Container Group.
        /// </summary>
        /// <param name="containerGroupName"></param>
        /// <returns>A response model containing details of the current Container Group.</returns>
        private async Task<ContainerGroup_GetResponseModel> GetContainerGroupDetails(string containerGroupName)
        {
            string getContainerGroupEndpoint = $"https://management.azure.com/subscriptions/{Config.AciSubscriptionId}/resourceGroups/{Config.AciResourceGroupName}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}?api-version=2021-09-01";

            try
            {
                IFlurlResponse response = await getContainerGroupEndpoint
                                                                .WithHeader("Authorization", $"Bearer {_aciToken}")
                                                                .GetAsync();

                ContainerGroup_GetResponseModel responseJson = await response.GetJsonAsync<ContainerGroup_GetResponseModel>();
                string responseString = await response.GetStringAsync();

                return responseJson;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception in ContainerizedAppLibApi.GetContainerGroupDetails: Error while attempting to get Container Group details.");
                return null;
            }
        }

        /// <summary>
        /// Gets logs from a running Container inside of a running Container Group.
        /// </summary>
        /// <param name="containerGroupName">Name of running Container Group.</param>
        /// <param name="containerName">Name of running Container inside Container Group.</param>
        /// <returns>Container logs in string format.</returns>
        public async Task<string> GetContainerLogs(string containerGroupName, string containerName)
        {
            string getContainerLogEndpoint = $"https://management.azure.com/subscriptions/{Config.AciSubscriptionId}/resourceGroups/{Config.AciResourceGroupName}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}/containers/{containerName}/logs?api-version=2021-09-01";

            try
            {
                IFlurlResponse response = await getContainerLogEndpoint
                                                                .WithHeader("Authorization", $"Bearer {_aciToken}")
                                                                .GetAsync();

                if (response.StatusCode == 200)
                {
                    Logs responseJson = await response.GetJsonAsync<Logs>();
                    return responseJson.Content;
                }
                else
                {
                    CloudError responseJson = await response.GetJsonAsync<CloudError>();
                    Log.Error($"Error obtaining container logs: {responseJson.Error.Message}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception from ContainerizedAppLibApi.GetContainerLogs: Error while attempting to get Container Logs for Container {containerName} in Container Group {containerGroupName}.");
                throw;
            }

            return null;
        }

        /// <summary>
        /// Lists all Container Groups belonging to the currently configured Resource Group.
        /// </summary>
        /// <returns>List of Container Groups.</returns>
        public async Task<List<ContainerGroup_GetResponseModel>> ListContainerGroupsInResourceGroup()
        {
            string listContainerGroupsInResourceGroupEndpoint = $"https://management.azure.com/subscriptions/{Config.AciSubscriptionId}/resourceGroups/{Config.AciResourceGroupName}/providers/Microsoft.ContainerInstance/containerGroups?api-version=2021-09-01";

            try
            {
                string response = await listContainerGroupsInResourceGroupEndpoint
                                    .WithHeader("Authorization", $"Bearer {_aciToken}")
                                    .GetStringAsync();

                var parsedResponse = JsonConvert.DeserializeObject<ListContainerGroup_GetResponseModel>(response);
                return parsedResponse.ContainerGroups;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception in ContainerizedAppLibApi.ListContainerGroupsInResourceGroup: Error fetching ACI Container Groups for resource group.");
                throw;
            }
        }

        /// <summary>
        /// Stop a currently running Container Group.
        /// </summary>
        /// <param name="containerGroupName">The name of the Container Group to be stopped.</param>
        /// <returns></returns>
        public async Task<bool> StopContainerInstance(string containerGroupName)
        {
            string stopContainerInstanceEndpoint = $"https://management.azure.com/subscriptions/{Config.AciSubscriptionId}/resourceGroups/{Config.AciResourceGroupName}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}/stop?api-version=2021-09-01";

            try
            {
                IFlurlResponse response = await stopContainerInstanceEndpoint
                                    .WithHeader("Authorization", $"Bearer {_aciToken}")
                                    .PostAsync();

                return response.StatusCode == 202;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception from ContainerizedAppLibApi.StopContainerInstance: Error when attempting to stop running ACI Container Group {containerGroupName}.");
                throw;
            }
        }

        /// <summary>
        /// Restart a previously existing Container Group.
        /// </summary>
        /// <param name="containerGroupName">The name of the Container Group to be restarted.</param>
        /// <returns></returns>
        public async Task<bool> RestartContainerGroup(string containerGroupName)
        {
            string restartContainerGroupEndpoint = $"https://management.azure.com/subscriptions/{Config.AciSubscriptionId}/resourceGroups/{Config.AciResourceGroupName}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}/restart?api-version=2021-09-01";

            try
            {
                IFlurlResponse response = await restartContainerGroupEndpoint
                                    .WithHeader("Authorization", $"Bearer {_aciToken}")
                                    .PostAsync();

                return response.StatusCode == 202;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception from ContainerizedAppLibApi.RestartContainerGroup: Error when attempting to restart ACI Container Group {containerGroupName}.");
                throw;
            }
        }

        /// <summary>
        /// Deletes a Container Group.
        /// </summary>
        /// <param name="containerGroupName">The name of the Container Group to delete.</param>
        /// <returns></returns>
        public async Task<bool> DeleteContainerGroup(string containerGroupName)
        {
            string restartContainerGroupEndpoint = $"https://management.azure.com/subscriptions/{Config.AciSubscriptionId}/resourceGroups/{Config.AciResourceGroupName}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}?api-version=2021-09-01";

            try
            {
                IFlurlResponse response = await restartContainerGroupEndpoint
                                    .WithHeader("Authorization", $"Bearer {_aciToken}")
                                    .DeleteAsync();

                return response.StatusCode == 202;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception from ContainerizedAppLibApi.DeleteContainerGroup: Error when attempting to delete ACI Container Group {containerGroupName}.");
                throw;
            }
        }
        #endregion
        
        class ACRAuthenticationResponse
        {
            [JsonProperty(PropertyName = "access_token")]
            public string AccessToken { set; internal get; }
        }

        class BlobData
        {
            [JsonProperty(PropertyName = "mediaType")]
            public string MediaType { set; internal get; }
            [JsonProperty(PropertyName = "size")]
            public int Size { set; internal get; }
            [JsonProperty(PropertyName = "digest")]
            public string Digest { set; internal get; }
        }
    }
}

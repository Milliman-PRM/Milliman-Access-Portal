using Flurl.Http;
using MapCommonLib;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Serilog;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using MapCommonLib.ContentTypeSpecific;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ContainerizedAppLib.AzureRestApiModels;

namespace ContainerizedAppLib
{
    public class ContainerizedAppLibApi : ContentTypeSpecificApiBase
    {
        public ContainerizedAppLibApiConfig Config { get; private set; }
        private string _acrToken, _aciToken, _repositoryName;

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
        /// Asynchronous initializer, chainable with the constructor
        /// </summary>
        /// <returns></returns>
        public async Task<ContainerizedAppLibApi> InitializeAsync(string repositoryName)
        {
            _repositoryName = repositoryName;

            try
            {
                await GetAcrAccessTokenAsync(repositoryName);
                await GetAciAccessTokenAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obtaining ContainerizedAppLibApi authentication token");
            }

            return this;
        }

        /// <summary>
        /// Initialize a new access token
        /// </summary>
        /// <returns></returns>
        private async Task<bool> GetAcrAccessTokenAsync(string repositoryName)
        {
            string tokenEndpointWithScope = $"{Config.ContainerRegistryTokenEndpoint}&scope=repository:{repositoryName}:pull,push,delete";
            try
            {
                ACRAuthenticationResponse response = await StaticUtil.DoRetryAsyncOperationWithReturn<Exception, ACRAuthenticationResponse>(async () =>
                                                        await tokenEndpointWithScope
                                                            .WithHeader("Authorization", $"Basic {Config.ContainerRegistryCredentialBase64}")
                                                            .GetAsync()
                                                            .ReceiveJson<ACRAuthenticationResponse>(), 3, 100);

                _acrToken = response.AccessToken;
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception attempting to get ACR access token");
                throw;
            }
        }

        #region Container Registry

        public async Task<object> GetRepositoryManifest(string repositoryName, string tag = "latest")
        {
            string manifestEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/manifests/{tag}";
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
                Log.Warning(ex, "Exception attempting to fetch repository manifest.");
                throw;
            }
        }

        public async Task<List<string>> GetRepositoryTags(string repositoryName)
        {
            string manifestEndpoint = $"https://{Config.ContainerRegistryUrl}/v1/{repositoryName}/_tags";
            
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
                Log.Warning(ex, "Exception attempting to fetch repository manifest.");
                throw;
            }
        }

        private async Task DeleteRepositoryManifest(string repositoryName, string digest)
        {
            string deleteImageManifestEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/manifests/{digest}";
            try
            {
                await deleteImageManifestEndpoint
                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to delete image manifest for {repositoryName}:{digest}");
            }
        }

        public async Task DeleteRepository(string repositoryName)
        {
            string deleteRepositoryEndpoint = $"https://{Config.ContainerRegistryUrl}/acr/v1/{repositoryName}";
            try
            {
                await deleteRepositoryEndpoint
                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to delete repository {repositoryName}");
            }
        }

        private async Task DeleteTag(string repositoryName, string tag)
        {
            string deleteTagEndpoint = $"https://{Config.ContainerRegistryUrl}/acr/v1/{repositoryName}/_tags/{tag}";
            try
            {
                await deleteTagEndpoint
                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to delete image tag for {repositoryName}:{tag}");
            }
        }

        public async Task PushImageManifest(string repositoryName, string manifestContents, string tag)
        {
            string manifestUploadEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/manifests/{tag}";

            try
            {
                IFlurlResponse manifestUploadResponse = await manifestUploadEndpoint
                                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                                    .WithHeader("Content-Type", "application/vnd.docker.distribution.manifest.v2+json")
                                    .PutStringAsync(manifestContents);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception attempting to upload a new image manifest.");
                throw;
            }
        }

        public async Task PushImageToRegistry(string imageFileFullPath, string repositoryName, string tag = "latest")
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
                    if (!await BlobDoesExist(repositoryName, $"sha256:{blobDigest}"))
                    {
                        string blobPath = Path.Combine(workingFolderName, blobDigest);
                        await UploadBlob(repositoryName, blobDigest, blobPath);
                    }
                }

                await PushImageManifest(repositoryName, manifestContents, tag);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to push image file {imageFileFullPath} to Azure registry");
                throw;
            }
        }

        private async Task<bool> BlobDoesExist(string repositoryName, string blobDigest)
        {
            string checkExistenceEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/blobs/{blobDigest}";

            try
            {
                IFlurlResponse response = await checkExistenceEndpoint
                                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                                    .HeadAsync();

                response.Headers.TryGetFirst("Docker-Content-Digest", out string responseDigest);
                return response.StatusCode == 202 && responseDigest.Equals(blobDigest, StringComparison.InvariantCultureIgnoreCase);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception when checking existence of layer.");
                return false;
            }
        }

        private async Task UploadBlob(string repositoryName, string layerDigest, string pathToLayer)
        {
            string nextUploadLocation = "";
            IFlurlRequest startBlobUploadEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/blobs/uploads/"
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
                                if (!builder.ToString().Equals(layerDigest))
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
                    string finishBlobUploadEndpoint = $"https://{Config.ContainerRegistryUrl}{nextUploadLocation}&digest=sha256:{layerDigest}";
                    IFlurlResponse endUploadResponse = await finishBlobUploadEndpoint
                                                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                                                    .PutAsync();
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to upload layer.");
                throw;
            }
        }

        public async Task RetagImage(string oldTag, string newTag, bool deleteOldTag = true)
        {
            // Get existing manifest.
            object manifestObj = await GetRepositoryManifest(_repositoryName, oldTag);
            string parsedManifestString = JsonConvert.SerializeObject(manifestObj, Formatting.None);

            // Push same manifest with new tag.
            await PushImageManifest(_repositoryName, parsedManifestString, newTag);

            // Remove previously tagged image.
            if (deleteOldTag)
            {
                await DeleteTag(_repositoryName, oldTag);
            }
        }
        #endregion

        #region Container Instances
        public async Task<bool> GetAciAccessTokenAsync()
        {
            try
            {
                MicrosoftAuthenticationResponse response = await StaticUtil.DoRetryAsyncOperationWithReturn<Exception, MicrosoftAuthenticationResponse>(async () =>
                                                                        await Config.ContainerInstanceTokenEndpoint
                                                                        .PostMultipartAsync(mp => mp
                                                                            .AddString("grant_type", Config.AciGrantType)
                                                                            .AddString("scope", Config.AciScope)
                                                                            .AddString("client_id", Config.AciClientId)
                                                                            .AddString("client_secret", Config.AciClientSecret)
                                                                        )
                                                                        .ReceiveJson<MicrosoftAuthenticationResponse>(), 3, 100);

                if (response.ExpiresIn > 0 && response.ExtExpiresIn > 0)
                {
                    _aciToken = response.AccessToken;
                    return true;
                }
                else
                {
                    Log.Warning("Invalid response when authenticating to Azure Container Instances, response object is {@response}", response);
                    response = null;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception attempting to get Azure Container Instances access token");
                throw;
            }

            return false;
        }

        public async Task<string> RunContainer(string containerGroupName, 
                                               string containerImageName, 
                                               string containerImageTag, 
                                               string ipType, 
                                               int cpuCoreCount, 
                                               double memorySizeInGB, 
                                               string vnetId, 
                                               string vnetName, 
                                               params ushort[] containerPorts)
        {
            try
            {
                string imagePath = $"{Config.ContainerRegistryUrl}/{containerImageName}:{containerImageTag}";

                bool createResult = await CreateContainerGroup(containerGroupName, imagePath, ipType, cpuCoreCount, memorySizeInGB, vnetId, vnetName, containerPorts);

                if (createResult)
                {
                    DateTime timeLimit = DateTime.UtcNow + TimeSpan.FromMinutes(5);
                    string containerGroupProvisioningState = null;
                    string containerGroupIpAddress = null;
                    ushort applicationPort = 0;
                    string containerGroupInstanceViewState = null;
                    ContainerGroup_GetResponseModel containerGroupModel = default;

                    while (DateTime.UtcNow < timeLimit && new[] { null, "Pending", "Creating" }.Contains(containerGroupProvisioningState))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        containerGroupModel = await GetContainerGroupStatus(containerGroupName);

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

#warning This waits until the application in the container has launched/initialized.  How much time is enough, different applications have different initializations

                    string containerLogMatchString = "Listening on http";

                    if (!string.IsNullOrEmpty(containerLogMatchString))
                    {
                        string log = string.Empty;
                        for (System.Diagnostics.Stopwatch logTimer = new System.Diagnostics.Stopwatch();
                             logTimer.Elapsed < TimeSpan.FromSeconds(60) && !log.Contains(containerLogMatchString, StringComparison.InvariantCultureIgnoreCase);
                             log = await GetContainerLogs(containerGroupName, containerGroupName))
                        {
                            Log.Information($"Container logs: {log}");
                            await Task.Delay(TimeSpan.FromSeconds(0.5));
                        }
                    }

                    return $"http://{containerGroupIpAddress}:{applicationPort}";
                }
            }
            catch (Exception ex)
            {
                //Log.Error(ex, "");
            }

            return "";
        }

        public async Task<bool> CreateContainerGroup(string containerGroupName, string containerImageName, string ipType, int cpuCoreCount, double memorySizeInGB, string vnetId = null, string vnetName = null, params ushort[] containerPorts)
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
                    }
                };

                string serializedRequestModel = JsonConvert.SerializeObject(requestModel);
                IFlurlResponse response = await createContainerGroupEndpoint
                                .WithHeader("Authorization", $"Bearer {_aciToken}")
                                .WithHeader("Content-Type", "application/json")
                                .PutJsonAsync(requestModel);

                var x = response.GetStringAsync();

                return response.StatusCode == 201;
            }
            catch (FlurlHttpException ex)
            {
                dynamic result = await ex.GetResponseJsonAsync();
                Dictionary<string, object> error = ((IDictionary<string, object>)result.error).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                string errorMessage = $"Error launching a new Container Group.  Error(s):{Environment.NewLine}\t" +
                                      string.Join($"{Environment.NewLine}\t", error.Select(kvp => $"{kvp.Key}:{kvp.Value}"));

                Log.Error(ex, errorMessage);
                throw new ApplicationException(errorMessage);
            }
        }

        private async Task<ContainerGroup_GetResponseModel> GetContainerGroupStatus(string containerGroupName)
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
                Log.Error(ex, $"Exception while attempting to get status of container group.");
                return null;
            }
        }

        public async Task<string> GetContainerLogs(string containerGroupName, string containerName)
        {
            string getContainerGroupEndpoint = $"https://management.azure.com/subscriptions/{Config.AciSubscriptionId}/resourceGroups/{Config.AciResourceGroupName}/providers/Microsoft.ContainerInstance/containerGroups/{containerGroupName}/containers/{containerName}/logs?api-version=2021-09-01";

            try
            {
                IFlurlResponse response = await getContainerGroupEndpoint
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
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception while attempting to get container logs.");
                return null;
            }
        }
        public async Task<object> ListContainerGroupsInResourceGroup() // todo redefine return type
        {
            string listContainerGroupsInResourceGroupEndpoint = $"https://management.azure.com/subscriptions/{Config.AciSubscriptionId}/resourceGroups/{Config.AciResourceGroupName}/providers/Microsoft.ContainerInstance/containerGroups?api-version=2021-09-01";

            try
            {
                IFlurlResponse response = await listContainerGroupsInResourceGroupEndpoint
                                    .WithHeader("Authorization", $"Bearer {_aciToken}")
                                    .GetAsync();

                return await response.GetJsonAsync().Result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception when attempting to list all ACI Container Groups in Resource Group.");
                return null;
            }
        }

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
                Log.Error(ex, $"Exception when attempting to stop running ACI Container Group {containerGroupName}.");
                return false;
            }
        }

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
                Log.Error(ex, $"Exception when attempting to restart ACI Container Group {containerGroupName}.");
                return false;
            }
        }

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
                Log.Error(ex, $"Exception when attempting to delete ACI Container Group {containerGroupName}.");
                return false;
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
using Flurl.Http;
using Azure;
using Azure.Core;
using Azure.Containers.ContainerRegistry;
using Azure.Containers.ContainerRegistry.Specialized;
using Azure.Identity;
using MapCommonLib;
using Microsoft.Azure.Management.ContainerRegistry;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Serilog;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using MapCommonLib.ContentTypeSpecific;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System.Linq;

namespace ContainerizedAppLib
{
    public class ContainerizedAppLibApi : ContentTypeSpecificApiBase
    {
        public ContainerizedAppLibApiConfig Config { get; private set; }
        private IAzure _azureContext;
        private ContainerRegistryClient _containerRegistryClient;
        private string _acrToken;

        public override Task<UriBuilder> GetContentUri(string typeSpecificContentIdentifier, string UserName, HttpRequest thisHttpRequest)
        {
            throw new NotImplementedException();
        }

        public ContainerizedAppLibApi(ContainerizedAppLibApiConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Asynchronous initializer, chainable with the constructor
        /// </summary>
        /// <returns></returns>
        public async Task<ContainerizedAppLibApi> InitializeAsync()
        {
            try
            {
                await GetAccessTokenAsync();
                GetAzureContextForContainerInstances();

                ContainerRegistryClient client = new ContainerRegistryClient(
                    new Uri(Config.ContainerRegistryUrl),
                    new DefaultAzureCredential(), // TODO 
                    new ContainerRegistryClientOptions() { Audience = ContainerRegistryAudience.AzureResourceManagerPublicCloud }
                );
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obtaining DockerLibApi authentication token");
            }

            return this;
        }

        /// <summary>
        /// Initialize a new access token
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GetAccessTokenAsync()
        {
            // It may be possible to replace this with something that uses package:  Microsoft.IdentityModel.Clients.ActiveDirectory
            try
            {
                ACRAuthenticationResponse response = await StaticUtil.DoRetryAsyncOperationWithReturn<Exception, ACRAuthenticationResponse>(async () =>
                                                                        await Config.ContainerRegistryTokenEndpoint
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

        public async Task<List<ContainerRepository>> GetRepositories()
        {
            try
            {
                AsyncPageable<string> repositoryNames = _containerRegistryClient.GetRepositoryNamesAsync();

                List<ContainerRepository> containerRepositories = new List<ContainerRepository>();
                await foreach (string repositoryName in repositoryNames)
                {
                    ContainerRepository repository = _containerRegistryClient.GetRepository(repositoryName);
                    containerRepositories.Add(repository);
                }

                return containerRepositories;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception attempting to fetch repositories.");
                throw;
            }
        }

        public async Task<JObject> GetRepositoryManifest(string repositoryName, string tag = "latest")
        {
            string manifestEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/manifests/{tag}";
            try
            {
                JObject response = await StaticUtil.DoRetryAsyncOperationWithReturn<Exception, JObject>(async () =>
                                    await manifestEndpoint
                                        .WithHeader("Authorization", $"Bearer {_acrToken}")
                                        .WithHeader("Accept", "application/vnd.oci.image.manifest.v2+json")
                                        .GetAsync()
                                        .ReceiveJson<JObject>(), 3, 100);
                return response;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception attempting to fetch repository manifest.");
                throw;
            }
        }

        public async Task PushImageManifest(string repositoryName, JObject manifestContents, string reference)
        {
            var manifestUploadEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/manifests/{reference}";

            try
            {
                var manifestUploadResponse = await manifestUploadEndpoint
                                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                                    .WithHeader("Content-Type", "application/vnd.docker.distribution.manifest.v2+json")
                                    .PutJsonAsync(manifestContents);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception attempting to upload a new image manifest.");
                throw;
            }
        }

        public async Task<JObject> PushImageToRegistry(string repositoryName, string manifestPath, string imageDigest, string imagePath)
        {
            #region Compile layers
            List<string> layerNames;
            JObject manifestObj;
            FileStream fs = File.OpenRead(manifestPath);
            using (StreamReader streamReader = new StreamReader(fs))
            {
                string fileContents = streamReader.ReadToEnd().Trim(new char[] { '[', ']' });
                manifestObj = JObject.Parse(fileContents);
                layerNames = manifestObj.SelectToken("Layers").ToObject<List<string>>();
            }
            #endregion

            try
            {
                foreach (string layerName in layerNames)
                {
                    string layerDigest = $"sha256:{Path.GetFileNameWithoutExtension(layerName)}";
                    if (!(await LayerDoesExist(repositoryName, layerDigest)))
                    {
                        await UploadLayer(repositoryName, layerDigest, imageDigest, imagePath, manifestObj);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception attempting to push an image.");
                throw;
            }

            return null;
        }

        private async Task<bool> LayerDoesExist(string repositoryName, string layerDigest)
        {
            string checkExistenceEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/blobs/{layerDigest}";

            try
            {
                var response = await checkExistenceEndpoint
                                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                                    .HeadAsync();

                // TODO??: Add check for Content-Length and Docker-Content-Digest headers as well.
                return response.StatusCode == 202;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception when checking existence of layer.");
                // throw;
            }

            return false;
        }

        private async Task UploadLayer(string repositoryName, string layerDigest, string imageDigest, string pathToLayer, JObject manifestContents)
        {
            int chunkSize = 65_536;
            string nextUploadLocation = "";
            IFlurlRequest startBlobUploadEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/blobs/uploads/"
                .WithHeader("Authorization", $"Bearer {_acrToken}");

            try
            {
                #region Start blob upload.
                var response = await startBlobUploadEndpoint
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
                        (int, int) range = (0, 0);
                        MemoryStream stream = new MemoryStream();
                        BinaryReader binaryReader = new BinaryReader(new FileStream(pathToLayer, FileMode.Open, FileAccess.Read)); // disp
                        BinaryWriter binaryWriter = new BinaryWriter(stream);

                        while (true)
                        {
                            range.Item2 = range.Item1 + chunkSize;
                            byte[] buffer = binaryReader.ReadBytes(chunkSize);
                            if (buffer.Length == 0)
                            {
                                break;
                            }

                            binaryWriter.Write(buffer);
                            string chunkValue = Convert.ToBase64String(buffer, 0, buffer.Length); // b64
                            var content = new StreamContent(stream);

                            // todo: try monolithic
                            string blobUploadEndpoint = $"https://{Config.ContainerRegistryUrl}{nextUploadLocation}";
                            var blobUploadResponse = await blobUploadEndpoint
                                .WithHeader("Authorization", $"Bearer {_acrToken}")
                                .WithHeader("Accept", "application/vnd.oci.image.manifest.v2+json")
                                .WithHeader("Accept", "application/vnd.docker.distribution.manifest.v2+json")
                                .WithHeader("Access-Control-Expose-Headers", "Docker-Content-Digest")
                                .WithHeader("Content-Length", buffer.Length)
                                .WithHeader("Content-Type", "application/octet-stream")
                                .PatchStringAsync(chunkValue);
                            blobUploadResponse.Headers.TryGetFirst("Location", out nextUploadLocation);

                            range.Item1 = range.Item2;
                        }
                    }
                    #endregion

                    #region Finish blob upload.
                    string finishBlobUploadEndpoint = $"https://{Config.ContainerRegistryUrl}{nextUploadLocation}&digest={layerDigest}";
                    var endUploadResponse = await finishBlobUploadEndpoint
                                                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                                                    .PutAsync();
                    #endregion

                    await PushImageManifest(repositoryName, manifestContents, imageDigest);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to upload layer.");
                throw;
            }
        }
        #endregion

        #region Container Instances
        public void GetAzureContextForContainerInstances()
        {
            try
            {
                var creds = new AzureCredentialsFactory().FromServicePrincipal(Config.ACIClientId, Config.ACIClientSecret, Config.ACITenantId, AzureEnvironment.AzureGlobalCloud);
                _azureContext = Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds).WithSubscription(Config.ACISubscriptionId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error trying to create valid Azure context.");
            }
        }

        public async Task CreateContainerGroup(string containerGroupName, string containerImageName, double cpuCoreCount = 1.0, double memorySizeInGB = 1.0, params int[] containerPorts)
        {
            try
            {
                IResourceGroup resourceGroup = await _azureContext.ResourceGroups.GetByNameAsync(Config.ACIResourceGroupName);
                Region azureRegion = resourceGroup.Region;

                List<int> allPorts = new List<int>();
                int[] defaultPorts = new int[] { 80 };
                allPorts.AddRange(defaultPorts);
                allPorts.AddRange(containerPorts);

                var newContainerGroup = _azureContext.ContainerGroups.Define(containerGroupName)
                    .WithRegion(azureRegion)
                    .WithExistingResourceGroup(Config.ACIResourceGroupName)
                    .WithLinux()
                    .WithPrivateImageRegistry(Config.ContainerRegistryUrl, Config.ContainerRegistryUsername, Config.ContainerRegistryPassword)
                    .WithoutVolume()
                    .DefineContainerInstance(containerGroupName)
                        .WithImage(containerImageName)
                        .WithExternalTcpPorts(allPorts.ToArray())
                        .WithCpuCoreCount(cpuCoreCount)
                        .WithMemorySizeInGB(memorySizeInGB)
                        .Attach()
                    .WithDnsPrefix(containerGroupName)
                    .Create();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error trying to create a new Container Group.");
            }
        }

        public async Task<string> GetContainerGroupStatus(IAzure azure, string containerGroupId)
        {
            IContainerGroup containerGroup;
            try
            {
                containerGroup = await azure.ContainerGroups.GetByIdAsync(containerGroupId);
                if (containerGroup != null)
                {
                    return containerGroup.State;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error trying to find an Azure Container Group.");
            }

            return "Not found";
        }

        public async Task StopContainerInstance(IAzure azure, string containerGroupId)
        {
            IContainerGroup containerGroup;
            try
            {
                containerGroup = await azure.ContainerGroups.GetByIdAsync(containerGroupId);
                if (containerGroup != null)
                {
                    await containerGroup.StopAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error trying to stop a running Container Group.");
            }
        }

        public async Task RestartContainerGroup(IAzure azure, string containerGroupId)
        {
            IContainerGroup containerGroup;
            try
            {
                containerGroup = await azure.ContainerGroups.GetByIdAsync(containerGroupId);
                if (containerGroup != null)
                {
                    await containerGroup.RestartAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error trying to stop a running Container Group.");
            }
        }
        #endregion

        class ACRAuthenticationResponse
        {
            [JsonProperty(PropertyName = "access_token")]
            public string AccessToken { set; internal get; }
        }
    }
}
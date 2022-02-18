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
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ContainerizedAppLib
{
    public class ContainerizedAppLibApi : ContentTypeSpecificApiBase
    {
        public ContainerizedAppLibApiConfig Config { get; private set; }
        private ContainerRegistryClient _containerRegistryClient;
        private string _acrToken, _repositoryName;
        
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
                await GetAccessTokenAsync(repositoryName);
                ContainerRegistryClient client = new ContainerRegistryClient(
                    new Uri(Config.ContainerRegistryUrl),
                    new DefaultAzureCredential(), // TODO 
                    new ContainerRegistryClientOptions() { Audience = ContainerRegistryAudience.AzureResourceManagerPublicCloud }
                );
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
        private async Task<bool> GetAccessTokenAsync(string repositoryName)
        {
            string tokenEndpointWithScope = $"{Config.ContainerRegistryTokenEndpoint}&scope=repository:{repositoryName}:pull,push"; // TODO get different permission tokens
            try
            {
                ACRAuthenticationResponse response = await StaticUtil.DoRetryAsyncOperationWithReturn<Exception, ACRAuthenticationResponse>(async () =>
                                                        await tokenEndpointWithScope
                                                            .WithHeader("Authorization", $"Basic {Config.ContainerRegistryCredential}")
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

        public async Task PushImageManifest(string repositoryName, string manifestContents, string reference)
        {
            var manifestUploadEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/manifests/{reference}";

            try
            {
                var manifestUploadResponse = await manifestUploadEndpoint
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

        public async Task<JObject> PushImageToRegistry(string repositoryName, string imageDigest, string imagePath)
        {
            #region Compile layers
            List<string> layerDigests = new List<string>();
            JObject manifestObj;

            var manifestPath = Path.Combine(imagePath, "manifest.json");
            if (!Directory.Exists(imagePath))
            {
                throw new Exception($"Image path cannot be found at {imagePath}");
            }
            if (!File.Exists(manifestPath))
            {
                throw new Exception($"Invalid image format: Manifest cannot be found for image located at {imagePath}.");
            }

            FileStream fs = File.OpenRead(manifestPath);
            string manifestContents = "";
            using (StreamReader streamReader = new StreamReader(fs))
            {
                manifestContents = streamReader.ReadToEnd().Trim(new char[] { '[', ']' });
                manifestObj = JObject.Parse(manifestContents);
                var layers = manifestObj.SelectToken("layers").ToObject<List<Layer>>();
                layerDigests = layers.Select(layer => layer.Digest.Replace("sha256:", "")).ToList();
            }
            #endregion

            try
            {
                foreach (string layerDigest in layerDigests)
                {
                    if (!(await LayerDoesExist(repositoryName, layerDigest)))
                    {
                        var layerPath = Path.Combine(imagePath, layerDigest);
                        await UploadLayer(repositoryName, layerDigest, layerPath);
                    }
                }

                await PushImageManifest(repositoryName, manifestContents, imageDigest);
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

        private async Task UploadLayer(string repositoryName, string layerDigest, string pathToLayer)
        {
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
                        byte[] rawFileBytes = File.ReadAllBytes(pathToLayer); // TODO adjust this to handle a stream.

                        using (var hasher = SHA256.Create())
                        {
                            StringBuilder builder = new StringBuilder();
                            byte[] result = hasher.ComputeHash(rawFileBytes);
                            foreach (byte b in result)
                            {
                                builder.Append(b.ToString("x2"));
                            }
                            if (!builder.ToString().Equals(layerDigest))
                            {
                                throw new Exception($"Error on pushing image: Calculated SHA256 hash does not match given layer digest.");
                            }
                        }

                        while (true)
                        {
                            /** TODO: implement chunking
                            byte[] buffer = binaryReader.ReadBytes(chunkSize);
                            if (buffer.Length == 0)
                            {
                                break;
                            }
                            **/

                            string blobUploadEndpoint = $"https://{Config.ContainerRegistryUrl}{nextUploadLocation}";
                            string base64String = Convert.ToBase64String(rawFileBytes);
                            var blobUploadResponse = await blobUploadEndpoint
                                .WithHeader("Authorization", $"Bearer {_acrToken}")
                                .WithHeader("Accept", "application/vnd.oci.image.manifest.v2+json")
                                .WithHeader("Accept", "application/vnd.docker.distribution.manifest.v2+json")
                                .WithHeader("Access-Control-Expose-Headers", "Docker-Content-Digest")
                                .WithHeader("Content-Length", rawFileBytes.Length)
                                .WithHeader("Content-Type", "application/octet-stream")
                                .PatchAsync(new ByteArrayContent(rawFileBytes));
                            blobUploadResponse.Headers.TryGetFirst("Location", out nextUploadLocation);

                            break;
                        }
                    }
                    #endregion

                    #region Finish blob upload.
                    string finishBlobUploadEndpoint = $"https://{Config.ContainerRegistryUrl}{nextUploadLocation}&digest=sha256:{layerDigest}";
                    var endUploadResponse = await finishBlobUploadEndpoint
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

        class ACRAuthenticationResponse
        {
            [JsonProperty(PropertyName = "access_token")]
            public string AccessToken { set; internal get; }
        }

        class Layer
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
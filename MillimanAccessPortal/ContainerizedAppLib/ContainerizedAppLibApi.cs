using Flurl.Http;
using Azure.Containers.ContainerRegistry;
using Azure.Identity;
using MapCommonLib;
using Microsoft.Azure.Management.ContainerRegistry;
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

namespace ContainerizedAppLib
{
    public class ContainerizedAppLibApi : ContentTypeSpecificApiBase
    {
        public ContainerizedAppLibApiConfig Config { get; private set; }
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
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obtaining Azure Container Registry authentication token");
            }

            return this;
        }

        /// <summary>
        /// Initialize a new access token
        /// </summary>
        /// <returns></returns>
        private async Task<bool> GetAccessTokenAsync(string repositoryName)
        {
            string tokenEndpointWithScope = $"{Config.ContainerRegistryTokenEndpoint}&scope=repository:{repositoryName}:pull,push";
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

        public async Task PushImageManifest(string repositoryName, string manifestContents, string tag)
        {
            var manifestUploadEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/manifests/{tag}";

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

        public async Task PushImageToRegistry(string repositoryName, string imagePath, string tag = "latest")
        {
            #region Compile layers
            List<string> blobDigests = new List<string>();
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
                List<BlobData> allBlobData = manifestObj.SelectToken("layers").ToObject<List<BlobData>>();
                BlobData configObject = manifestObj.SelectToken("config").ToObject<BlobData>();
                blobDigests = allBlobData
                                .Select(layerData => layerData.Digest.Replace("sha256:", "")).ToList()
                                .Append(configObject.Digest.Replace("sha256:", "")).ToList(); // Include config BLOB to create a new repository.
            }
            #endregion

            try
            {
                foreach (string blobDigest in blobDigests)
                {
                    if (!await BlobDoesExist(repositoryName, $"sha256:{blobDigest}"))
                    {
                        var blobPath = Path.Combine(imagePath, blobDigest);
                        await UploadBlob(repositoryName, blobDigest, blobPath);
                    }
                }

                await PushImageManifest(repositoryName, manifestContents, tag);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception attempting to push an image.");
                throw;
            }
        }

        private async Task<bool> BlobDoesExist(string repositoryName, string blobDigest)
        {
            string checkExistenceEndpoint = $"https://{Config.ContainerRegistryUrl}/v2/{repositoryName}/blobs/{blobDigest}";

            try
            {
                var response = await checkExistenceEndpoint
                                    .WithHeader("Authorization", $"Bearer {_acrToken}")
                                    .HeadAsync();

                response.Headers.TryGetFirst("Docker-Content-Digest", out string responseDigest);
                return response.StatusCode == 202 && responseDigest.Equals(blobDigest);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception when checking existence of layer.");
            }

            return false;
        }

        private async Task UploadBlob(string repositoryName, string layerDigest, string pathToLayer)
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
                        using (FileStream fileStream = new FileStream(pathToLayer, FileMode.Open, FileAccess.Read))
                        {
                            // Do a hash check on the BLOB to ensure that upload of layer data occurs in an OCI compliant way.
                            using (var hasher = SHA256.Create())
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
                                var blobUploadResponse = await blobUploadEndpoint
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
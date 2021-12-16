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

namespace DockerLib
{
    public class DockerLibApi
    {
        public DockerLibApiConfig Config { get; set; }
        private string _acrToken { get; set; }

        public DockerLibApi(DockerLibApiConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Asynchronous initializer, chainable with the constructor
        /// </summary>
        /// <returns></returns>
        public async Task<DockerLibApi> InitializeAsync()
        {
            try
            {
                await GetAccessTokenAsync();
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
            string manifestEndpoint = $"https://{Config.RegistryUrl}/v2/{repositoryName}/manifests/{tag}";
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
            var manifestUploadEndpoint = $"https://{Config.RegistryUrl}/v2/{repositoryName}/manifests/{reference}";

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

        public async Task<JObject> PushImageToRegistry(string repositoryName)
        {
            // temp
            string hardCodedRepoName = "new-repo";
            string hardCodedManifest = "{\"Config\":\"feb5d9fea6a5e9606aa995e879d862b825965ba48de054caab5ef356dc6b3412.json\",\"RepoTags\":[\"prmcontainertest.azurecr.io / hello - world:latest\"],\"Layers\":[\"e07ee1baac5fae6a26f30cabfe54a36d3402f96afda318fe0a96cec4ca393359.tar\"]}";
            string hardCodedLayerDigest = "sha256:e07ee1baac5fae6a26f30cabfe54a36d3402f96afda318fe0a96cec4ca393359";
            string hardCodedImageDigest = "sha256:b9935d4e8431fb1a7f0989304ec86b3329a99a25f5efdc7f09f3f8c41434ca6d";
            string hardCodedLayerLocation = @"C:\Users\Evan.Klein\source\Misc\hello-world\e07ee1baac5fae6a26f30cabfe54a36d3402f96afda318fe0a96cec4ca393359.tar";
            string hardCodedManifestLocation = @"C:\Users\Evan.Klein\source\Misc\hello-world\manifest.json";

            #region Compile layers
            List<string> layerNames;
            FileStream fs = File.OpenRead(hardCodedManifestLocation);
            using (StreamReader streamReader = new StreamReader(fs))
            {
                string fileContents = streamReader.ReadToEnd().Trim(new char[] { '[', ']' });
                JObject manifestJson = JObject.Parse(fileContents);
                layerNames = manifestJson.SelectToken("Layers").ToObject<List<string>>();
            }
            #endregion

            try
            {
                foreach (string layerName in layerNames)
                {
                    string digest = layerName.Substring(0, layerName.Length - 5); // Strips .tar
                    await UploadLayer(hardCodedRepoName, hardCodedLayerDigest, digest, hardCodedLayerLocation, JObject.Parse(hardCodedManifest));
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
            string checkExistenceEndpoint = $"https://{Config.RegistryUrl}/v2/{repositoryName}/blobs/{layerDigest}";

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
            string nextUploadLocation = "", nextUploadUuid = "";
            IFlurlRequest startBlobUploadEndpoint = $"https://{Config.RegistryUrl}/v2/{repositoryName}/blobs/uploads/"
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
                        BinaryReader binaryReader = new BinaryReader(new FileStream(pathToLayer, FileMode.Open, FileAccess.Read));
                        BinaryWriter binaryWriter = new BinaryWriter(stream);

                        while (true)
                        {
                            range.Item2 = range.Item1 + chunkSize;
                            byte[] buffer = binaryReader.ReadBytes(chunkSize);
                            if (buffer.Length == 0)
                            {
                                binaryWriter.Close();
                                break;
                            }

                            binaryWriter.Write(buffer);
                            string chunkValue = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);

                            string blobUploadEndpoint = $"https://{Config.RegistryUrl}{nextUploadLocation}";
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
                    string finishBlobUploadEndpoint = $"https://{Config.RegistryUrl}{nextUploadLocation}&digest={layerDigest}";
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

        public async Task Demonstrate()
        {
            try
            {
                // Initialize.
                ContainerRegistryClient client = new ContainerRegistryClient(new Uri(Config.RegistryUrl), 
                    new DefaultAzureCredential(), 
                    new ContainerRegistryClientOptions()
                    {
                        Audience = ContainerRegistryAudience.AzureResourceManagerPublicCloud,
                    }
                );

                // Get list of all repositories contained inside defined registry.
                AsyncPageable<string> repositoryNames = client.GetRepositoryNamesAsync();

                // Compile all repository details into a defined list.
                List<ContainerRepository> containerRepositories = new List<ContainerRepository>();
                await foreach (string repositoryName in repositoryNames)
                {
                    ContainerRepository repository = client.GetRepository(repositoryName);
                    containerRepositories.Add(repository);
                }

                // Grab a random container repo (first is fine).
                ContainerRepository containerRepository = containerRepositories[0];
                ContainerRegistryBlobClient blobClient = new ContainerRegistryBlobClient(new Uri(Config.RegistryUrl), new DefaultAzureCredential(), containerRepository.Name, new ContainerRegistryClientOptions()
                {
                    Audience = ContainerRegistryAudience.AzureResourceManagerPublicCloud,
                });

                Pageable<ArtifactManifestProperties> imageManifests = containerRepository.GetManifestPropertiesCollection(orderBy: ArtifactManifestOrderBy.LastUpdatedOnDescending);
                foreach (ArtifactManifestProperties imageManifest in imageManifests)
                {
                    RegistryArtifact image = containerRepository.GetArtifact(imageManifest.Digest);
                    var manifestResponse = blobClient.DownloadManifest(new DownloadManifestOptions(digest: imageManifest.Digest));
                    var blobResponse = blobClient.DownloadBlob(imageManifest.Digest);

                    // Download Blob to a local destination
                    using (FileStream file = new FileStream("C:\\Users\\Evan.Klein\\Documents\\BlobFile", FileMode.Create, System.IO.FileAccess.Write))
                    {
                        byte[] bytes = new byte[blobResponse.Value.Content.Length];
                        blobResponse.Value.Content.Read(bytes, 0, (int) blobResponse.Value.Content.Length);
                        file.Write(bytes, 0, bytes.Length);
                        blobResponse.Value.Content.Close();
                    }

                    foreach (var tagName in imageManifest.Tags)
                    {
                        Console.WriteLine($"{imageManifest.RepositoryName}:{tagName}");

                        // image.DeleteTag(tagName); // Deletes tag.
                    }
                    // image.Delete(); // Delete image altogether.
                }
            }
            catch (Exception ex)
            {
                ;
            }
        }

        class ACRAuthenticationResponse
        {
            [JsonProperty(PropertyName = "access_token")]
            public string AccessToken { set; internal get; }
        }
    }
}
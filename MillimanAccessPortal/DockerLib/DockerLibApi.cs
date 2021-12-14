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

namespace DockerLib
{
    public class DockerLibApi
    {
        public DockerLibApiConfig Config { get; set; }
        private TokenCredentials _tokenCredentials { get; set; }

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
                _tokenCredentials = new TokenCredentials(response.AccessToken);
                return true;
            }
            #region exception handling
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception attempting to get ACR access token");
                throw;
            }
            #endregion

            return false;
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
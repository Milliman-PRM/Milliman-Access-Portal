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

namespace DockerLib
{
    public class DockerLibApi
    {
        public DockerLibApiConfig Config { get; set; }

        public async Task Demonstrate()
        {
            #region Auth

            // Hardcoded.
            Config = new DockerLibApiConfig()
            {
                RegistryUrl = "https://evkleincontainerregistry.azurecr.io",
            };

            #endregion

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
                Console.WriteLine(ex.Message);
            }

        }
    }

    public class AuthenticationAccessResponse
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken;
    }

    public class RegistryTokenCredential : TokenCredential
    {
        public AccessToken Token { get; set; }
        public RegistryTokenCredential(string Token)
        {
            this.Token = new AccessToken(Token, DateTimeOffset.MaxValue);
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return Token;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
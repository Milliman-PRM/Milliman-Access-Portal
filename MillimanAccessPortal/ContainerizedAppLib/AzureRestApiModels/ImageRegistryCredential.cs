using Newtonsoft.Json;

namespace ContainerizedAppLib.AzureRestApiModels
{
    // https://docs.microsoft.com/en-us/rest/api/container-instances/container-groups/create-or-update#imageregistrycredential
    public class ImageRegistryCredential
    {
        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }

        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }

        [JsonProperty(PropertyName = "server")]
        public string Server { get; set; }

        /*
         * For managed Identity
         * 
         * [JsonProperty(PropertyName = "identity")]
         * public string Identity { get; set; }

         * [JsonProperty(PropertyName = "identityUrl")]
         * public string IdentityUrl { get; set; }
        */
    }
}

namespace ContainerizedAppLib
{
    public class ContainerizedAppLibApiConfig
    {
        public string ContainerRegistryUrl { get; set; }

        public string ContainerRegistryCredential { get; set; }
        public string ContainerRegistryScope { get; set; }

        public string ContainerRegistryTokenEndpoint
        {
            get => $"https://{ContainerRegistryUrl}/oauth2/token?service={ContainerRegistryUrl}";
        }
    }
}

namespace ContainerizedAppLib
{
    public class ContainerizedAppLibApiConfig
    {
        public string RegistryUrl { get; set; }

        public string ContainerRegistryCredential { get; set; }
        public string ContainerRegistryScope { get; set; }

        public string ContainerRegistryTokenEndpoint
        {
            get => $"https://{RegistryUrl}/oauth2/token?service={RegistryUrl}&scope={ContainerRegistryScope}";
        }
    }
}

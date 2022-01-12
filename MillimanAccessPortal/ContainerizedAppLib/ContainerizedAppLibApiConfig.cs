namespace ContainerizedAppLib
{
    public class ContainerizedAppLibApiConfig
    {
        public string ContainerRegistryUrl { get; set; }

        public string ContainerRegistryUsername { get; set; }
        public string ContainerRegistryPassword { get; set; }
        public string ContainerRegistryCredentialBase64
        {
            get => System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{ContainerRegistryUsername}:{ContainerRegistryPassword}"));
        }
        public string ContainerRegistryScope { get; set; }
        public string ContainerRegistryTokenEndpoint
        {
            get => $"https://{ContainerRegistryUrl}/oauth2/token?service={ContainerRegistryUrl}&scope={ContainerRegistryScope}";
        }
    }
}

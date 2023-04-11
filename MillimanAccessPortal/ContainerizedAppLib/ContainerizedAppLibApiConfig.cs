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
        public string ContainerRegistryTokenEndpoint
        {
            get => $"https://{ContainerRegistryUrl}/oauth2/token?service={ContainerRegistryUrl}";
        }
        public string ContainerInstanceTokenEndpoint
        {
            get => $"https://login.microsoftonline.com/{MapManagedAzureResourcesTenantId}/oauth2/v2.0/token";
        }
        public string MapClientResourcesResourceGroupName { get; set; }
        public string MapManagedAzureResourcesClientId { get; set; }
        public string MapManagedAzureResourcesClientSecret { get; set; }
        public string MapManagedAzureResourcesTenantId { get; set; }
        public string MapManagedAzureResourcesSubscriptionId { get; set; }
        public string MapManagedAzureResourcesGrantType { get; set; }
        public string AciApiVersion { get; set; } = "2021-09-01";
    }
}

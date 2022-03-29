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
            get => $"https://{ContainerRegistryUrl}/oauth2/token?service={ContainerRegistryUrl}";
        }
        public string ContainerInstanceTokenEndpoint
        {
            get => $"https://login.microsoftonline.com/{AciTenantId}/oauth2/v2.0/token";
        }
        public string AciResourceGroupName { get; set; }
        public string AciClientId { get; set; }
        public string AciClientSecret { get; set; }
        public string AciTenantId { get; set; }
        public string AciSubscriptionId { get; set; }
        public string AciScope { get; set; }
        public string AciGrantType { get; set; }
        public string AciApiVersion { get; set; } = "2021-09-01";
    }
}

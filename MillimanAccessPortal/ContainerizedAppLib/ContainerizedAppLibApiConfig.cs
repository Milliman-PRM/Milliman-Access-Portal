namespace ContainerizedAppLib
{
    public class ContainerizedAppLibApiConfig
    {
        internal string ContainerRegistryUrl { get; set; }
        internal string ContainerRegistryUsername { get; set; }
        internal string ContainerRegistryPassword { get; set; }
        internal string ContainerRegistryCredentialBase64
        {
            get => System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{ContainerRegistryUsername}:{ContainerRegistryPassword}"));
        }
        internal string ContainerRegistryScope { get; set; }
        internal string ContainerRegistryTokenEndpoint
        {
            get => $"https://{ContainerRegistryUrl}/oauth2/token?service={ContainerRegistryUrl}";
        }
        internal string ContainerInstanceTokenEndpoint
        {
            get => $"https://login.microsoftonline.com/{AciTenantId}/oauth2/v2.0/token";
        }
        internal string AciResourceGroupName { get; set; }
        internal string AciClientId { get; set; }
        internal string AciClientSecret { get; set; }
        internal string AciTenantId { get; set; }
        internal string AciSubscriptionId { get; set; }
        internal string AciScope { get; set; }
        internal string AciGrantType { get; set; }
        internal string AciApiVersion { get; set; } = "2021-09-01";
    }
}

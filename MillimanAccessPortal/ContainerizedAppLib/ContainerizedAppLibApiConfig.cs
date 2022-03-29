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
            get => $"https://login.microsoftonline.com/{ACITenantId}/oauth2/v2.0/token";
        }
        public string ACIResourceGroupName { get; set; }
        public string ACIClientId { get; set; }
        public string ACIClientSecret { get; set; }
        public string ACITenantId { get; set; }
        public string ACISubscriptionId { get; set; }
        public string ACIScope { get; set; }
        public string ACIGrantType { get; set; }

        public string ACIApiVersion { get; set; } = "2021-09-01";
    }
}

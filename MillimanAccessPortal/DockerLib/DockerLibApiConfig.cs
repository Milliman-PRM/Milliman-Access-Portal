using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DockerLib
{
    public class DockerLibApiConfig
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

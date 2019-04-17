using System;
using System.Collections.Generic;
using System.Text;

namespace PowerBILib
{
    public class PowerBIConfig
    {
        public string pbiGrantType { get; set; }

        public string pbiAuthenticationScope { get; set; }

        public string pbiAzureADClientId { get; set; }

        public string pbiAzureADClientSecret { get; set; }

        public string pbiAzureADUsername { get; set; }

        public string pbiAzureADPassword { get; set; }

        public string pbiTenantId { get; set; }

        public string pbiTokenEndpoint { get; set; }
    }
}

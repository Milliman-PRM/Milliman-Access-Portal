/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Configuration values utilized by PowerBiLibApi
 * DEVELOPER NOTES:
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PowerBiLib
{
    public class PowerBiConfig
    {
        public string PbiGrantType { get; set; }

        public string PbiAuthenticationScope { get; set; }

        public string PbiAzureADClientId { get; set; }

        public string PbiAzureADClientSecret { get; set; }

        public string PbiAzureADUsername { get; set; }

        public string PbiAzureADPassword { get; set; }

        public string PbiTenantId { get; set; }

        public string PbiCapacityId { get; set; }

        public string PbiTokenEndpoint
        {
            get => $"https://login.microsoftonline.com/{PbiTenantId}/oauth2/v2.0/token";
        }
    }
}
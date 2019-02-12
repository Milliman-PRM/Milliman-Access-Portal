using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace MillimanAccessPortal
{
    public class WsFederationConfigList
    {
        public List<WsFederationConfig> Items { get; set; }
    }
    public class WsFederationConfig
    {
        static readonly List<string> _requiredKeys = new List<string>
        {
            "Scheme",
            "MetadataAddress",
            "Wtrealm"
        };

        public string Scheme { get; set; }
        public string DisplayName { get; set; }
        public string MetadataAddress { get; set; }
        public string Wtrealm { get; set; }

        /// <summary>
        /// Binds a ConfigurationSection to a new instance of this class by field name
        /// Any fields not found will be set to null
        /// </summary>
        /// <param name="section"></param>
        public static explicit operator WsFederationConfig(ConfigurationSection section)
        {
            var missingKeys = _requiredKeys.Except(section.GetChildren().Select(s => s.Key));
            if (missingKeys.Any())
            {
                string Msg = $"WsFederation configuration is missing key(s) {string.Join(",", missingKeys)}";
                throw new ApplicationException(Msg);
            }

            var returnVal = new WsFederationConfig();
            section.Bind(returnVal);

            return returnVal;
        }
    }
}

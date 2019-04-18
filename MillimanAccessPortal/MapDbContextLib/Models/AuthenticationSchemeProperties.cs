/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Base class declaration and derived types representing configuration properties specific to each supported external authentication scheme
 * DEVELOPER NOTES: Persistence of properties of these types happens in entity AuthenticationScheme, jsonb field 
 * SchemeProperties, and can be accessed more directly through property SchemePropertiesObj
 */

using System.ComponentModel.DataAnnotations;

namespace MapDbContextLib.Models
{
    /// <summary>
    /// Base class for all authentication handler specific properties
    /// </summary>
    public class AuthenticationSchemeProperties
    {}

    public class WsFederationSchemeProperties : AuthenticationSchemeProperties
    {
        /// <summary>
        /// The relying party identifier used by the authenticating system to identify a configured trust for this integration
        /// </summary>
        [Required]
        public string Wtrealm { get; set; }

        /// <summary>
        /// The Federation Metadata endpoint URL served by the authenticating system
        /// </summary>
        [Required]
        public string MetadataAddress { get; set; }

        /// <summary>
        /// A URL formatted identifier for a specific authentication method (must be enabled on the domain)
        /// For values supported by Microsoft ADFS (subset of Ws-Federation):
        /// <see cref="https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-mwbf/77c337e9-e11c-4747-a3cd-ea8faebc9496#Appendix_A_14"/>
        /// </summary>
        public string Wauth { get; set; } = null;
    }
}

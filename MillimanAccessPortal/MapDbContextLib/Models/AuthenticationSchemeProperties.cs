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
        [Required]
        public string Wtrealm { get; set; }
        [Required]
        public string MetadataAddress { get; set; }
    }
}

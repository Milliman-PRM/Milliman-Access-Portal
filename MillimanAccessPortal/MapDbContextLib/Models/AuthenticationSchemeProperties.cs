/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
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

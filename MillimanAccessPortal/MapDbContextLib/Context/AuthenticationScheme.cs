/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: A database entity representing an authentication scheme that is configured and available in the system for user authentication.
 * DEVELOPER NOTES: The default scheme is initialized if not found at application launch. Other stored records are used at launch to 
 * initialize the in-memory AuthenticationService
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace MapDbContextLib.Context
{
    public enum AuthenticationType
    {
        // Warning.  If the value set is changed, a manual update is required to the enumeration type in the database, or raw SQL added using a migration
        Default,
        WsFederation,
    }

    public class AuthenticationScheme
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        // Unique constraint is imposed using fluent API in ApplicationDbContext class
        public string Name { get; set; }

        [Required]
        public AuthenticationType Type { get; set; }

        /// <summary>
        /// Storage of scheme handler specific properties.  Can be accessed using member <see cref="SchemePropertiesObj"/>
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string SchemeProperties { get; set; }

        [NotMapped]
        public AuthenticationSchemeProperties SchemePropertiesObj
        {
            get
            {
                switch (Type)
                {
                    case AuthenticationType.WsFederation:
                        return JsonConvert.DeserializeObject<WsFederationSchemeProperties>(SchemeProperties);

                    case AuthenticationType.Default:
                        return null;

                    default:
                        throw new ApplicationException($"Authentication scheme type {Type} is not handled in the property getter of {nameof(AuthenticationScheme)}.{nameof(SchemePropertiesObj)}");
                }
            }
            set
            {
                SchemeProperties = JsonConvert.SerializeObject(value);
            }
        }

        public string DisplayName { get; set; }

        [Required]
        //[Column(TypeName = "citext[]")]  TODO Use this to replace use of below method DomainListContains() when the EF provider starts to support this field type in a collection
        public List<string> DomainList { get; set; } = new List<string>();

        public bool DomainListContains(string testDomain)
        {
            return DomainList.Any(d => EF.Functions.ILike(d, testDomain));
        }

        #region Database initialization and validation
        /// <summary>
        /// Populate the Identity database with the roles in NamedRoles. This should likely only be called from Startup.cs
        /// </summary>
        /// <param name="serviceProvider">Application Services provide connectivity to the identity database.</param>
        /// <returns></returns>
        internal static async Task SeedSchemes(IServiceProvider serviceProvider)
        {
            ApplicationDbContext dbContext = serviceProvider.GetService<ApplicationDbContext>();
            AuthenticationService authService = (AuthenticationService)serviceProvider.GetService<IAuthenticationService>();

            string defaultSchemeName = (await authService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name;
            if (!dbContext.AuthenticationScheme.Any(s => EF.Functions.ILike(s.Name, defaultSchemeName)))
            {
                AuthenticationScheme newScheme = new AuthenticationScheme
                {
                    Name = defaultSchemeName,
                    DisplayName = "Local MAP Authentication",
                    SchemeProperties = null,
                };
                await dbContext.AuthenticationScheme.AddAsync(newScheme);
                dbContext.SaveChanges();
            }
        }
        #endregion

    }
}

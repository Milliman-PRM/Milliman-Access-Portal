/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace MapDbContextLib.Context
{
    public class AuthenticationScheme
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string DisplayName { get; set; }

        [Required]
        public string MetadataAddress { get; set; }

        [Required]
        public string Wtrealm { get; set; }

        [Required]
        [Column(TypeName = "citext[]")]
        public List<string> DomainList { get; set; } = new List<string>();

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

            string defaultScheme = (await authService.Schemes.GetDefaultAuthenticateSchemeAsync()).Name;
            if (!dbContext.AuthenticationScheme.Any(s => s.Name == defaultScheme))
            {
                AuthenticationScheme newScheme = new AuthenticationScheme
                {
                    Name = defaultScheme,
                    DisplayName = "Local MAP Authentication",
                    MetadataAddress = null,
                    Wtrealm = null,
                };
                await dbContext.AuthenticationScheme.AddAsync(newScheme);
                dbContext.SaveChanges();
            }
        }
        #endregion

    }
}

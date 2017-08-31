/*
 * CODE OWNERS: Tom Puckett, Ben Wyatt
 * OBJECTIVE: Provide extensions to the base IdentityRole class
 * DEVELOPER NOTES: When adding a new named role, add a new static property that defines the role name, 
 *                      then add the property to the NamedRoles array.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;

namespace MapDbContextLib.Identity
{
    public class ApplicationRole : IdentityRole<long>
    {
        public ApplicationRole() : base() { }

        public ApplicationRole(string RoleName) : base(RoleName) { }

        #region Role definitions and management
        public static readonly string SuperUser = "Super User";
        public static readonly string ClientAdministrator = "Client Administrator";
        public static readonly string UserManager = "User Manager";
        public static readonly string ContentPublisher = "Content Publisher";
        public static readonly string ContentUser = "Content User";
        
        public static readonly string[] NamedRoles = { SuperUser, ClientAdministrator, UserManager, ContentPublisher, ContentUser };

        /// <summary>
        /// Populate the Identity database with the roles in NamedRoles. This should likely only be called from Startup.cs
        /// </summary>
        /// <param name="serviceProvider">Application Services provide connectivity to the identity database.</param>
        /// <returns></returns>
        internal static void SeedRoles(IServiceProvider serviceProvider)
        {
            using (IServiceScope serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetService<Context.ApplicationDbContext>();
                var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

                foreach (string role in NamedRoles)
                {
                    if (!roleManager.RoleExistsAsync(role).Result)
                    {
                        roleManager.CreateAsync(new ApplicationRole(role)).Wait();
                    }
                }
            }
        }
        #endregion
    }
}

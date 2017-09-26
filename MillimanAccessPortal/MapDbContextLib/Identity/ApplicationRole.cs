/*
 * CODE OWNERS: Tom Puckett, Ben Wyatt
 * OBJECTIVE: Provide extensions to the base IdentityRole class
 * DEVELOPER NOTES: When adding a new named role, add a new static property that defines the role name, 
 *                      then add the property to the NamedRoles array.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Context;

namespace MapDbContextLib.Identity
{
    public enum RoleEnum : int
    {
        SuperUser = 1,
        ClientAdministrator = 2,
        UserManager = 3,
        ContentPublisher = 4,
        ContentUser = 5,
    };

    public class ApplicationRole : IdentityRole<long>
    {
        public readonly static Dictionary<RoleEnum,string> MapRoles = new Dictionary<RoleEnum, string>
            {
                {RoleEnum.SuperUser, "Super User"},
                {RoleEnum.ClientAdministrator, "Client Administrator"},
                {RoleEnum.UserManager, "User Manager"},
                {RoleEnum.ContentPublisher, "Content Publisher"},
                {RoleEnum.ContentUser, "Content User"},
            };

        /// <summary>
        /// Used for initialization to ensure explicit assignment of role names to enumeration values
        /// </summary>
        public ApplicationRole() : base() { }

        public ApplicationRole(string RoleName) : base(RoleName) { }

        public RoleEnum RoleEnum { get; set; }

        #region Database initialization and validation
        /// <summary>
        /// Populate the Identity database with the roles in NamedRoles. This should likely only be called from Startup.cs
        /// </summary>
        /// <param name="serviceProvider">Application Services provide connectivity to the identity database.</param>
        /// <returns></returns>
        internal static void SeedRoles(IServiceProvider serviceProvider)
        {
            using (IServiceScope serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                using (ApplicationDbContext dbContext = serviceScope.ServiceProvider.GetService<ApplicationDbContext>())
                {
                    foreach (KeyValuePair<RoleEnum, string> Role in MapRoles)
                    {
                        ApplicationRole RoleFromDb = roleManager.FindByNameAsync(Role.Value).Result;
                        if (RoleFromDb == null)
                        {
                            roleManager.CreateAsync(new ApplicationRole { Name = Role.Value, RoleEnum = Role.Key }).Wait();
                        }
                        else if (RoleFromDb.RoleEnum != Role.Key)
                        {
                            RoleFromDb.RoleEnum = Role.Key;
                            roleManager.UpdateAsync(RoleFromDb).Wait();
                        }
                    }

                    // Read back all role records from persistence
                    Dictionary<RoleEnum, string> FoundRoleNames = new Dictionary<RoleEnum, string>();
                    foreach (ApplicationRole R in dbContext.ApplicationRole)
                    {
                        FoundRoleNames.Add(R.RoleEnum, R.Name);
                    }

                    // Make sure the database table contains exactly the expected records and no more
                    if (!MapRoles.OrderBy(mr=>mr.Key).SequenceEqual(FoundRoleNames.OrderBy(fr => fr.Key)))
                    {
                        throw new Exception("Failed to correctly initialize Roles in database.");
                    }
                }
            }
        }
        #endregion
    }
}

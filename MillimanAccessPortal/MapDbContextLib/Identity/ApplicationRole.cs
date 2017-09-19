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
        public static Dictionary<RoleEnum,string> LiveRoles = new Dictionary<RoleEnum, string>();

        /// <summary>
        /// Used for initialization to ensure explicit assignment of role names to enumeration values
        /// </summary>
        public ApplicationRole() : base() { }

        public ApplicationRole(string RoleName) : base(RoleName) { }

        public RoleEnum RoleEnum { get; set; }

        #region Role definitions and management
        /// <summary>
        /// Populate the Identity database with the roles in NamedRoles. This should likely only be called from Startup.cs
        /// </summary>
        /// <param name="serviceProvider">Application Services provide connectivity to the identity database.</param>
        /// <returns></returns>
        internal static void SeedRoles(IServiceProvider serviceProvider)
        {
            // The ApplicationRole
            // This construct enforces that the numeric and string values are consistently paired during db initialization. 
            Dictionary<RoleEnum, string> RoleNameDict = new Dictionary<RoleEnum, string>
            {
                {RoleEnum.SuperUser, "Super User"},
                {RoleEnum.ClientAdministrator, "Client Administrator"},
                {RoleEnum.UserManager, "User Manager"},
                {RoleEnum.ContentPublisher, "Content Publisher"},
                {RoleEnum.ContentUser, "Content User"},
            };

            using (IServiceScope serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                using (ApplicationDbContext dbContext = serviceScope.ServiceProvider.GetService<ApplicationDbContext>())
                {
                    foreach (KeyValuePair<RoleEnum, string> Role in RoleNameDict)
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

                    foreach (ApplicationRole R in dbContext.ApplicationRole.OrderBy(r => r.Id))
                    {
                        LiveRoles.Add(R.RoleEnum, R.Name);
                    }

                    // Make sure the database table contains exactly the expected records
                    if (LiveRoles.Except(RoleNameDict).Count() > 0)
                    {
                        throw new Exception("Failed to correctly initialize Roles in database.");
                    }
                }
            }
        }
        #endregion
    }
}

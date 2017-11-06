/*
 * CODE OWNERS: Tom Puckett, Ben Wyatt
 * OBJECTIVE: Provide extensions to the base IdentityRole class
 * DEVELOPER NOTES: When adding a new named role, add the role to the RoleEnum enumeration,
 *                      then add a new dictionary element that defines the role name.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Context;

namespace MapDbContextLib.Identity
{
    public enum RoleEnum : long  // Inherited type must be same as ApplicationRole.Id
    {
        // Important: Existing numeric values must never be reassigned to a new meaning.  Always add a new role as a new, explicit, higher value. 
        SystemAdmin = 1,
        ClientAdmin = 2,
        UserAdmin = 3,
        ContentPublisher = 4,
        ContentUser = 5,
        RootClientCreator = 6,
        UserCreator = 7,
        ContentAdmin = 8,
    };

    public class ApplicationRole : IdentityRole<long>
    {
        public readonly static Dictionary<RoleEnum,string> MapRoles = new Dictionary<RoleEnum, string>
            {
                {RoleEnum.SystemAdmin, "System Administrator"},
                {RoleEnum.ClientAdmin, "Client Administrator"},
                {RoleEnum.UserAdmin, "User Administrator"},
                {RoleEnum.ContentPublisher, "Content Publisher"},
                {RoleEnum.ContentUser, "Content User"},
                {RoleEnum.RootClientCreator, "Root Client Creator"},
                {RoleEnum.UserCreator, "User Creator"},
                {RoleEnum.ContentAdmin, "Content Administrator"},
            };

        /// <summary>
        /// Used for initialization to ensure explicit assignment of role names to enumeration values
        /// </summary>
        public ApplicationRole() : base() { }

        public ApplicationRole(string RoleName) : base(RoleName) { }

        [NotMapped]
        public RoleEnum RoleEnum
        {
            get
            {
                return (RoleEnum)Id;
            }
            set
            {
                Id = (long)value;  // Cast must be to type of this.Id
            }
        }

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
                var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                using (ApplicationDbContext dbContext = serviceScope.ServiceProvider.GetService<ApplicationDbContext>())
                {
                    foreach (KeyValuePair<RoleEnum, string> Role in MapRoles)
                    {
                        ApplicationRole RoleFromDb = roleManager.FindByIdAsync(((long)Role.Key).ToString()).Result;
                        if (RoleFromDb == null)
                        {
                            roleManager.CreateAsync(new ApplicationRole { Name = Role.Value, RoleEnum = Role.Key }).Wait();
                        }
                        else if (RoleFromDb.Name != Role.Value)
                        {
                            RoleFromDb.Name = Role.Value;
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

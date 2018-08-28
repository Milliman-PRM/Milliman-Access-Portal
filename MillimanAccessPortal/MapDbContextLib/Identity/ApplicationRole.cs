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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Context;

namespace MapDbContextLib.Identity
{
    public enum RoleEnum
    {
        // Important: Existing numeric values must never be reassigned to a new meaning.  Always add a new role as a new, explicit, higher value. 
        Admin = 1,
        UserCreator = 2,
        ContentAccessAdmin = 3,
        ContentPublisher = 4,
        ContentUser = 5,
    };

    public class ApplicationRole : IdentityRole<Guid>
    {
        public static Dictionary<RoleEnum, string> RoleDisplayNames = new Dictionary<RoleEnum, string>
        {
            { RoleEnum.Admin, "Admin"},
            { RoleEnum.UserCreator, "User Creator"},
            { RoleEnum.ContentAccessAdmin, "Content Access Admin"},
            { RoleEnum.ContentPublisher, "Content Publisher"},
            { RoleEnum.ContentUser, "Content User"},
        };

        public static Dictionary<RoleEnum, Guid> RoleIds = new Dictionary<RoleEnum, Guid>();

        /// <summary>
        /// Used for initialization to ensure explicit assignment of role names to enumeration values
        /// </summary>
        public ApplicationRole() : base() { }

        public ApplicationRole(string RoleName) : base(RoleName) { }

        public RoleEnum RoleEnum { get; set; }

        public string DisplayName { get; set; }

        #region Database initialization and validation
        /// <summary>
        /// Populate the Identity database with the roles in NamedRoles. This should likely only be called from Startup.cs
        /// </summary>
        /// <param name="serviceProvider">Application Services provide connectivity to the identity database.</param>
        /// <returns></returns>
        internal static async Task SeedRoles(IServiceProvider serviceProvider)
        {
            RoleManager<ApplicationRole> roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            ApplicationDbContext dbContext = serviceProvider.GetService<ApplicationDbContext>();

            foreach (RoleEnum Role in Enum.GetValues(typeof(RoleEnum)))
            {
                string RoleName = Role.ToString();
                ApplicationRole RecordFromDb = dbContext.ApplicationRole.SingleOrDefault(r => r.RoleEnum == Role);
                if (RecordFromDb == null)
                {
                    await roleManager.CreateAsync(new ApplicationRole { RoleEnum = Role, Name = RoleName, DisplayName = RoleDisplayNames[Role] });
                }
                else if (RecordFromDb.Name != RoleName)
                {
                    throw new Exception($"It is not possible to change ApplicationRole name in database from {RecordFromDb.Name} to {RoleName}.");
                }
                else if (RecordFromDb.DisplayName != RoleDisplayNames[Role])
                {
                    RecordFromDb.DisplayName = RoleDisplayNames[Role];
                    roleManager.UpdateAsync(RecordFromDb).Wait();
                }

                RoleIds[Role] = RecordFromDb.Id;
            }

            // Read back all role records from persistence
            Dictionary<RoleEnum, string> FoundRolesInDb = new Dictionary<RoleEnum, string>();
            Dictionary<RoleEnum, string> FoundDisplayNamesInDb = new Dictionary<RoleEnum, string>();
            foreach (ApplicationRole R in dbContext.ApplicationRole)
            {
                FoundRolesInDb.Add(R.RoleEnum, R.Name);
                FoundDisplayNamesInDb.Add(R.RoleEnum, R.DisplayName);
            }

            // Make sure the database table contains exactly the expected records and no more
            var RoleNamesInitialized = Enum.GetValues(typeof(RoleEnum)).Cast<RoleEnum>().OrderBy(r => r).Select(r => new KeyValuePair<RoleEnum, string>(r, r.ToString()));
            var RoleDisplayNamesInitialized = Enum.GetValues(typeof(RoleEnum)).Cast<RoleEnum>().OrderBy(r => r).Select(r => new KeyValuePair<RoleEnum, string>(r, RoleDisplayNames[r]));
            if (!RoleNamesInitialized.SequenceEqual(FoundRolesInDb.OrderBy(fr => fr.Key)) ||
                !RoleDisplayNamesInitialized.SequenceEqual(FoundDisplayNamesInDb.OrderBy(fr => fr.Key)))
            {
                throw new Exception("ApplicationRole records in database are not as expected after initialization.");
            }
        }
        #endregion
    }
}

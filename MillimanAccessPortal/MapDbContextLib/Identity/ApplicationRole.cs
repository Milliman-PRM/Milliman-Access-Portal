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
    public enum RoleEnum : long  // Inherited type must be same as ApplicationRole.Id
    {
        // Important: Existing numeric values must never be reassigned to a new meaning.  Always add a new role as a new, explicit, higher value. 
        Admin = 1,
        UserCreator = 2,
        UserAdmin = 3,
        ContentAdmin = 4,
        ContentUser = 5,
    };

    public class ApplicationRole : IdentityRole<long>
    {
        /// <summary>
        /// Used for initialization to ensure explicit assignment of role names to enumeration values
        /// </summary>
        public ApplicationRole() : base() { }

        public ApplicationRole(string RoleName) : base(RoleName) { }

        /// This overide is here only to apply the explicit [Key] attribute, required in unit tests
        [Key]
        public override long Id { get; set; }

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
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            ApplicationDbContext dbContext = serviceProvider.GetService<ApplicationDbContext>();

            foreach (RoleEnum Role in Enum.GetValues(typeof(RoleEnum)))
            {
                string RoleName = Role.ToString();
                ApplicationRole RoleFromDb = roleManager.FindByIdAsync(((long)Role).ToString()).Result;
                if (RoleFromDb == null)
                {
                    roleManager.CreateAsync(new ApplicationRole { Name = RoleName, RoleEnum = Role }).Wait();
                }
                else if (RoleFromDb.Name != RoleName)
                {
                    throw new Exception($"It is not possible to change ApplicationRole name in database from {RoleFromDb.Name} to {RoleName}.");
                }
            }

            // Read back all role records from persistence
            Dictionary<RoleEnum, string> FoundRolesInDb = new Dictionary<RoleEnum, string>();
            foreach (ApplicationRole R in dbContext.ApplicationRole)
            {
                FoundRolesInDb.Add(R.RoleEnum, R.Name);
            }

            // Make sure the database table contains exactly the expected records and no more
            var RolesInitialized = Enum.GetValues(typeof(RoleEnum)).Cast<RoleEnum>().OrderBy(r => r).Select(r => new KeyValuePair<RoleEnum, string>(r, r.ToString()));
            if (!RolesInitialized.SequenceEqual(FoundRolesInDb.OrderBy(fr => fr.Key)))
            {
                throw new Exception("ApplicationRole records in database are not as expected after initialization.");
            }
        }
        #endregion
    }
}

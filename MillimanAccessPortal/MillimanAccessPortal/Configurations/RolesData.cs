/*
 *  CODE OWNERS: Ben Wyatt
 *  
 *  OBJECTIVE: Provide a mechanism to seed the Identity database with the roles that are required for the application.
 * 
 */

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MillimanAccessPortal.Data;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.Configurations
{
    /// <summary>
    /// Creates Identity framework roles to be used by the application. To add new roles, simply add them to Roles below.
    /// 
    /// Shamelessly copied from https://stackoverflow.com/a/39934793/ and modified for our purposes    /// 
    /// </summary>
    public static class RolesData
    {
        private static readonly string[] Roles = new string[] { "Super User", "Client Administrator", "User Manager", "Content Publisher", "Content User" };

        public static async Task SeedRoles(IServiceProvider serviceProvider)
        {
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();

                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                foreach (var role in Roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }
            }
        }
    }
}

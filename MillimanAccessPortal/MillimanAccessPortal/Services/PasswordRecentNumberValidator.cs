/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Search a user's password history for prior usage of the password being set
 *              Returns a failed result and error message to be displayed to the user if the password was used within a specified number of passwords
 * DEVELOPER NOTES: To change the time interval, update the numberOfPasswords variable
 */

using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class PasswordRecentNumberValidator<TUser> : IPasswordValidator<TUser>
        where TUser : ApplicationUser
    {
        public int numberOfPasswords { get; set; } = 10;

        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            List<PreviousPassword> history = user.PasswordHistoryObj;
            
            // Check the specified number of recent passwords
            if (history.OrderByDescending(p => p.dateSet)
                                     .Take(numberOfPasswords)
                                     .Any(p => p.PasswordMatches(password)))
            {
                var result = IdentityResult.Failed(new IdentityError
                {
                    Code = "Password Reuse",
                    Description = $"You cannot reuse your {numberOfPasswords} most recent passwords"
                });

                return Task.FromResult(result);
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}

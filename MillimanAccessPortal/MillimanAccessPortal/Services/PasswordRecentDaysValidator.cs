/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Search a user's password history for prior usage of the password being set
 *              Returns a failed result and error message to be displayed to the user if the password was used within a specified number of days
 * DEVELOPER NOTES: To change the time interval, update the numberOfDays variable
 */

using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class PasswordRecentDaysValidator<TUser> : IPasswordValidator<TUser>
        where TUser : ApplicationUser
    {
        public int numberOfDays { get; set; } = 180;

        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            // If user is null, this check is likely for the account activation view
            if (user == null)
            {
                return Task.FromResult(IdentityResult.Success);
            }

            DateTime Cutoff = DateTime.UtcNow - new TimeSpan(numberOfDays, 0, 0, 0);
            
            // Check the specified number of days of history
            if (user.PasswordHistoryObj.Where(s => s.dateSetUtc > Cutoff)
                                     .Any(p => p.PasswordMatches(password)))
            {
                var result = IdentityResult.Failed(new IdentityError
                {
                    Code = "Password Reuse",
                    Description = $"You cannot reuse any password created in the last {numberOfDays} days"
                });

                return Task.FromResult(result);
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}

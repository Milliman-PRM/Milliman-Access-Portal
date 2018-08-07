/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Search a user's password history for prior usage of the password being set
 *              Returns a failed result and error message to be displayed to the user if the password was used within a specified number of days
 * DEVELOPER NOTES: To change the time interval, update the numberOfDays variable
 */

using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class PasswordRecentDaysValidator<TUser> : IPasswordValidator<TUser>
        where TUser : ApplicationUser
    {

        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            int numberOfDays = 180;

            DateTimeOffset beginningTime = DateTimeOffset.Now.Subtract(new TimeSpan(180, 0, 0, 0));
            
            // Check the specified number of days of history
            if (user.PasswordRecentlyUsed(password, beginningTime))
            {
                var result = IdentityResult.Failed(new IdentityError
                {
                    Code = "Password Reuse",
                    Description = $"You cannot reuse passwords used in the last {numberOfDays}"
                });

                return Task.FromResult(result);
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}

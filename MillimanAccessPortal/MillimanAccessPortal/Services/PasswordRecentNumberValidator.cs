/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Search a user's password history for prior usage of the password being set
 *              Returns a failed result and error message to be displayed to the user if the password was used within a specified number of passwords
 * DEVELOPER NOTES: To change the time interval, update the numberOfPasswords variable
 */

using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class PasswordRecentNumberValidator<TUser> : IPasswordValidator<TUser>
        where TUser : ApplicationUser
    {
        public int numberOfPasswords { get; set; } = 10;

        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            // Check the specified number of recent passwords
            if (user.WasPasswordUsedInLastN(password, numberOfPasswords))
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

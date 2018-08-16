/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Search a user's password history for prior usage of the password being set
 *              Returns a failed result and error message to be displayed to the user if the password exists in history
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class PasswordHistoryValidator<TUser> : IPasswordValidator<TUser>
        where TUser : ApplicationUser
    {

        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            if (user.PasswordHistoryObj.Any(p => p.PasswordMatches(password)))
            {
                var result = IdentityResult.Failed(new IdentityError
                {
                    Code = "Password Reuse",
                    Description = "You cannot a previous password"
                });

                return Task.FromResult(result);
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}

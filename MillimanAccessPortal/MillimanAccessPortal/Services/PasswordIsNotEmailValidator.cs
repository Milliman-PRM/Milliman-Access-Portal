using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class PasswordIsNotEmailValidator<TUser> : IPasswordValidator<TUser>
        where TUser : ApplicationUser
    {
        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            // If user is null, this check is likely for the account activation view
            if (user == null)
            {
                return Task.FromResult(IdentityResult.Success);
            }

            string normalizedPassword = manager.NormalizeName(password);
            
            // This case covers the user's initial password - May only be used by ~/Account/CreateInitialUser
            if (string.IsNullOrWhiteSpace(user.NormalizedEmail))
            {
                if (normalizedPassword.Contains(manager.NormalizeName(user.Email)))
                {
                    var result = IdentityResult.Failed(new IdentityError
                    {
                        Code = "Password Contains Email",
                        Description = $"Your password cannot contain your email address"
                    });

                    return Task.FromResult(result);
                }
            }
            else if (normalizedPassword.Contains(user.NormalizedEmail))
            {
                var result = IdentityResult.Failed(new IdentityError
                {
                    Code = "Password Contains Email",
                    Description = $"Your password cannot contain your email address"
                });

                return Task.FromResult(result);
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}

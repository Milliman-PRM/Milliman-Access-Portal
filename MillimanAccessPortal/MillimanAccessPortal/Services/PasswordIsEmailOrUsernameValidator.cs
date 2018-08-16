using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class PasswordIsEmailOrUsernameValidator<TUser> : IPasswordValidator<TUser>
        where TUser : ApplicationUser
    {
        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            string upperPassword = password.ToUpper();
            
            // This case covers the user's initial password - May only be used by ~/Account/Register
            if (string.IsNullOrWhiteSpace(user.NormalizedEmail) || string.IsNullOrWhiteSpace(user.NormalizedUserName))
            {
                if (upperPassword.Contains(user.Email.ToUpper()))
                {
                    var result = IdentityResult.Failed(new IdentityError
                    {
                        Code = "Password Contains Email or Username",
                        Description = $"Your password cannot contain your email address or username"
                    });

                    return Task.FromResult(result);
                }
            }
            else if (upperPassword.Contains(user.NormalizedEmail) || upperPassword.Contains(user.NormalizedUserName))
            {
                var result = IdentityResult.Failed(new IdentityError
                {
                    Code = "Password Contains Email or Username",
                    Description = $"Your password cannot contain your email address or username"
                });

                return Task.FromResult(result);
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}

using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class PasswordIsEmailValidator<TUser> : IPasswordValidator<TUser>
        where TUser : ApplicationUser
    {
        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            string upperPassword = password.ToUpper();
            
            if (upperPassword.Contains(user.NormalizedEmail) || upperPassword.Contains(user.NormalizedUserName))
            {
                var result = IdentityResult.Failed(new IdentityError
                {
                    Code = "Password Reuse",
                    Description = $"Your password cannot contain your email address or username"
                });

                return Task.FromResult(result);
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}

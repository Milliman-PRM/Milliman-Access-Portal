using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class PasswordContainsCommonWordsValidator<TUser> : IPasswordValidator<TUser>
        where TUser : ApplicationUser
    {
        public List<string> commonWords { get; set; }

        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            if (commonWords.Any(c => password.ToLower().Contains(c.ToLower())))
            {
                var result = IdentityResult.Failed(new IdentityError
                {
                    Code = "Password Common Words",
                    Description = $"Your password contains a disallowed word or phrase"
                });

                return Task.FromResult(result);
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }
}

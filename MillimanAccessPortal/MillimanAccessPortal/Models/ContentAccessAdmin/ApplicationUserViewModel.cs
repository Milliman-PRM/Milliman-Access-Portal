/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Identity;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    /// <summary>
    /// A POCO class representing a MAP user without the sensitive information from Identity
    /// </summary>
    public class ApplicationUserViewModel
    {
        public ApplicationUserViewModel()
        { }

        /// <summary>
        /// Type conversion constructor, converts an ApplicationUser to this type
        /// </summary>
        /// <param name="UserArg"></param>
        /// <param name="userManager">Provide this if an array of client membership Ids is to be assigned</param>
        public static async Task<ApplicationUserViewModel> New(ApplicationUser UserArg, UserManager<ApplicationUser> userManager = null)
        {
            ApplicationUserViewModel ReturnObject = new ApplicationUserViewModel
            {
                Id = UserArg.Id,
                UserName = UserArg.UserName,
                Email = UserArg.Email,
                FirstName = UserArg.FirstName,
                LastName = UserArg.LastName,
                PhoneNumber = UserArg.PhoneNumber,
                Employer = UserArg.Employer,
            };

            if (userManager != null)
            {
                IList<Claim> ClaimsForUserArg = await userManager.GetClaimsAsync(UserArg);
                ReturnObject.MemberOfClientIdArray = ClaimsForUserArg
                                                        .Where(c => c.Type == ClaimNames.ClientMembership.ToString())
                                                        .Select(c =>
                                                                {
                                                                    long.TryParse(c.Value, out long ClientIdOfClaim);
                                                                    return ClientIdOfClaim;
                                                                })
                                                        .ToArray();
            }

            return ReturnObject;
        }

        public long Id { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        public string Employer { get; set; }

        public long[] MemberOfClientIdArray { get; set; } = new long[0];

    }
}

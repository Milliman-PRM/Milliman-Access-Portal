/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Provide extensions to the base IdentityUser class
 * DEVELOPER NOTES: 
 */

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MapDbContextLib.Identity
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser<long>
    {
        public ApplicationUser() : base()
        {
        }

        public ApplicationUser(string userName) // : base(userName)
        {
            // The base class does not have a constructor taking an argument.  
            base.UserName = userName;
        }
        
    }
}

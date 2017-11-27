/*
 * CODE OWNERS: Ben Wyatt, Tom Puckett
 * OBJECTIVE: Provide extensions to the base IdentityUser class
 * DEVELOPER NOTES: 
 */

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

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

        /// This overide is here only to apply the explicit [Key] attribute, required in unit tests
        [Key]
        public override long Id { get; set; }

        //
        // Summary:
        //     Gets or sets the user's LastName.
        public virtual string LastName { get; set; }

        //
        // Summary:
        //     Gets or sets the user's FirstName.
        public virtual string FirstName { get; set; }

        //
        // Summary:
        //     Gets or sets the user's Employer.
        public virtual string Employer { get; set; }
    }
}

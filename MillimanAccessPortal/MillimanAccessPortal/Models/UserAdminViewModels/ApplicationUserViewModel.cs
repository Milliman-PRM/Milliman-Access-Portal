/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: MVC controller implementing handlers related to accessing hosted content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Models.UserAdminViewModels
{
    /// <summary>
    /// A POCO class representing a MAP user without the sensitive information from Identity
    /// </summary>
    public class ApplicationUserViewModel
    {
        public ApplicationUserViewModel()
        {}

        public ApplicationUserViewModel(ApplicationUser UserArg)
        {
            Id = UserArg.Id;
            UserName = UserArg.UserName;
            Email = UserArg.Email;
            FirstName = UserArg.FirstName;
            LastName = UserArg.LastName;
            PhoneNumber = UserArg.PhoneNumber;
            Employer = UserArg.Employer;
        }

        public long Id { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        public string Employer { get; set; }

    }
}

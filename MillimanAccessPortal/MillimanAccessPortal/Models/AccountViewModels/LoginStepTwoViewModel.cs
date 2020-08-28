/*
 * CODE OWNERS: Evan Klein, Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class LoginStepTwoViewModel
    {
        [Required]
        public string Username { get; set; }

        public string Code { get; set; }

        public string ReturnUrl { get; set; }

        public string UserMessage { get; set; } = null;

        public bool RememberMe { get; set; } = false;
    }
}

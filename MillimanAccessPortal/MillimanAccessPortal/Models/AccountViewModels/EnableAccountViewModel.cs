using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class EnableAccountViewModel
    {
        [Required]
        [HiddenInput]
        public long Id { get; set; }

        [Required]
        [HiddenInput]
        public string Code { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [Required]
        public string Employer { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }
    }
}

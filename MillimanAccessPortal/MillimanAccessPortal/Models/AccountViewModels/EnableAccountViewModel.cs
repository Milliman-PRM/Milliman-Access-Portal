using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class EnableAccountViewModel
    {
        [Required]
        [HiddenInput]
        public Guid Id { get; set; }

        [Required]
        [HiddenInput]
        public string Code { get; set; }

        [Required]
        [HiddenInput]
        public bool IsLocalAccount { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [Display(Name = "First Name *")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name *")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Phone Number *")]
        public string Phone { get; set; }

        [Required]
        [Display(Name = "Employer *")]
        public string Employer { get; set; }

        [Required]
        [Display(Name = "Time Zone *")]
        public string TimeZoneId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password *")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password *")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }

        // a hack to get jQuery validation to work with complex password requirements
        // relies on front end code to set this properly
        [Required(ErrorMessage = "The password does not satisfy complexity requirements.")]
        public string PasswordsAreValid { get; set; }
        public IEnumerable<TimeZoneInfo> TimeZoneSelections { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class LoginStepTwoViewModel
    {
        [Required]
        public string Username { get; set; }

        public string ReturnUrl { get; set; }

        [Required]
        public Boolean RememberMe { get; set; }
    }
}

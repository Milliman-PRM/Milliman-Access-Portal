using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class CheckPasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string ProposedPassword { get; set; }
    }
}

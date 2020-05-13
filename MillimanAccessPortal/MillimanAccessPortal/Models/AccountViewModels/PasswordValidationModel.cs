using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class PasswordValidationModel
    {
        public bool Valid { get; set; }

        public List<string> Messages { get; set; }
    }
}

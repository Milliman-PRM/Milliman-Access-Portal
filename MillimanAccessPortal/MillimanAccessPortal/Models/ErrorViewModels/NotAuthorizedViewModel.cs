using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ErrorViewModels
{
    public class NotAuthorizedViewModel
    {
        public string StackTrace { get; set; }
        public string Message { get; set; }
        public string ReturnToController { get; set; }
        public string ReturnToAction { get; set; }
    }
}

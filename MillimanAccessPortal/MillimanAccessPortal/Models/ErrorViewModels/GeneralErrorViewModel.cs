using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ErrorViewModels
{
    public class GeneralErrorViewModel
    {
        // Each element of the Message array is intended for display on a separate line
        public string[] Message { get; set; }
        public string ReturnToController { get; set; }
        public string ReturnToAction { get; set; }
    }
}

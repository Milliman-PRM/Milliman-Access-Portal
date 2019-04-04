using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class UserFullModel
    {
        public Guid Id;

        public bool IsActivated;

        public bool IsSuspended;

        public string FirstName;

        public string LastName;

        public string UserName;

        public string Email;

        public string Phone;

        public string Employer;

        public bool IsLocal;
    }
}

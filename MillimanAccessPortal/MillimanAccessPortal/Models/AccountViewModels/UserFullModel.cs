using System;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class UserFullModel
    {
        public Guid Id { get; set; }

        public bool IsActivated { get; set; }

        public bool IsSuspended { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Employer { get; set; }

        public bool IsLocal { get; set; }
    }
}

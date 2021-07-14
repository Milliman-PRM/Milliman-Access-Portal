namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class UpdateAccountModel
    {
        public class UserModel
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string Phone { get; set; }

            public string Employer { get; set; }

            public string TimeZoneSelected { get; set; }
        }

        public UserModel User { get; set; }
    }
}

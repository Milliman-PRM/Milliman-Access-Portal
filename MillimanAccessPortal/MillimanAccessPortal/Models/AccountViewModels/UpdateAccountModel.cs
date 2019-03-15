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
        }

        public class PasswordModel
        {
            public string Current { get; set; }

            public string New { get; set; }

            public string Confirm { get; set; }
        }

        public UserModel User { get; set; }

        public PasswordModel Password { get; set; }
    }
}

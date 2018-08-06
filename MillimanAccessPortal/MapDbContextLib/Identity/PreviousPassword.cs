using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MapDbContextLib.Identity
{
    public class PreviousPassword
    {
        public PreviousPassword()
        {
            EndDate = DateTimeOffset.Now;

        }

        [Key]
        public long UserId { get; set; }

        public string PasswordHash { get; set; }

        public DateTimeOffset EndDate { get; set; }

        public virtual ApplicationUser User { get; set; }
    }

}

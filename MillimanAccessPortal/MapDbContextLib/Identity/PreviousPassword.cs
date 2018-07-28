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
            CreateDate = DateTimeOffset.Now;

        }

        [Key]
        public long UserId { get; set; }

        public string PasswordHash { get; set; }

        public DateTimeOffset CreateDate { get; set; }

        public virtual ApplicationUser User { get; set; }
    }

}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing an account that will be authenticated by the MAP Sftp server
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class SftpAccount
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string UserName { get; set; }

        public string PasswordHash { get; set; }

        [Required]
        public DateTime PasswordResetDateTimeUtc { get; set; }

        [ForeignKey("ApplicationUser")]
        public Guid? ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [ForeignKey("FileDropUserPermissionGroup")]
        public Guid FileDropUserPermissionGroupId { get; set; }
        public FileDropUserPermissionGroup FileDropUserPermissionGroup { get; set; }

        public virtual ICollection<FileDropFile> Files { get; set; }
        public virtual ICollection<FileDropDirectory> Directories { get; set; }

        [NotMapped]
        public string Password {
            set
            {
                // TODO Use hashing, perhaps from Microsoft.AspNetCore.Identity.PasswordHasher<TUser>
            }
        }

    }
}

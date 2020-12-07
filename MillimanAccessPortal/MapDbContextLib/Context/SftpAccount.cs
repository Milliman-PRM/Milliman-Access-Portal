/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing an account that will be authenticated by the MAP Sftp server
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class SftpAccount
    {
        public SftpAccount(Guid fileDropId)
        {
            FileDropId = fileDropId;
        }

        [Key]
        public Guid Id { get; set; }

        [Required]
        public string UserName { get; set; }

        public string PasswordHash { get; set; }

        [Required]
        public DateTime PasswordResetDateTimeUtc { get; set; }

        public DateTime? LastLoginUtc { get; set; }

        [Required]
        public bool IsSuspended { get; set; } = false;

        [Column(TypeName = "jsonb")]
        public HashSet<FileDropUserNotificationModel> NotificationSubscriptions { get; set; } = new HashSet<FileDropUserNotificationModel>(new FileDropUserNotificationModelSameEventComparer());

        [ForeignKey("ApplicationUser")]
        public Guid? ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [ForeignKey("FileDropUserPermissionGroup")]
        public Guid? FileDropUserPermissionGroupId { get; set; }
        public FileDropUserPermissionGroup FileDropUserPermissionGroup { get; set; }

        [ForeignKey("FileDrop")]
        public Guid FileDropId { get; private set; }
        public FileDrop FileDrop { get; set; }

        public bool IsCurrent(int passwordExpiresDays) => !IsSuspended && (DateTime.UtcNow - PasswordResetDateTimeUtc < TimeSpan.FromDays(passwordExpiresDays));

        [NotMapped]
        public string Password {
            set
            {
                PasswordHash = GetPasswordHasher().HashPassword(this, value);
                PasswordResetDateTimeUtc = DateTime.UtcNow;
            }
        }

        public PasswordVerificationResult CheckPassword(string proposedPassword)
        {
            if (string.IsNullOrWhiteSpace(PasswordHash))
            {
                return PasswordVerificationResult.Failed;
            }

            try
            {
                PasswordVerificationResult verificationResult = GetPasswordHasher().VerifyHashedPassword(this, PasswordHash, proposedPassword);
                return verificationResult;
            }
            catch 
            {
                return PasswordVerificationResult.Failed;
            }

        }

        private static PasswordHasher<SftpAccount> GetPasswordHasher()
        {
            var options = new PasswordHasherOptions
            {
                IterationCount = 42_042,
            };
                
            return new PasswordHasher<SftpAccount>(new OptionsWrapper<PasswordHasherOptions>(options));
        }

    }
}

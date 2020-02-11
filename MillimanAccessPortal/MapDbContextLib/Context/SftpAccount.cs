/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing an account that will be authenticated by the MAP Sftp server
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
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

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public bool ReadAccess { get; set; } = false;

        [Required]
        public bool WriteAccess { get; set; } = false;

        [Required]
        public bool DeleteAccess { get; set; } = false;

        [ForeignKey("ApplicationUser")]
        public Guid? ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [ForeignKey("FileDrop")]
        public Guid FileDropId { get; set; }
        public FileDrop FileDrop { get; set; }

        [NotMapped]
        public string Password {
            set
            {
                PasswordHash = GetPasswordHasher().HashPassword(this, value);
            }
        }

        public PasswordVerificationResult CheckPassword(string proposedPassword)
        {
            var verificationResult = GetPasswordHasher().VerifyHashedPassword(this, PasswordHash, proposedPassword);

            return verificationResult;
        }

        private static PasswordHasher<SftpAccount> GetPasswordHasher()
        {
            OptionsWrapper<PasswordHasherOptions> optionsWrapper = new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions { IterationCount = 42_042,  });
            return new PasswordHasher<SftpAccount>(optionsWrapper);
        }

    }
}

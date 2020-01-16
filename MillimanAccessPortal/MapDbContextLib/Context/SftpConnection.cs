/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a account connection to a FileDrop on the MAP Sftp server
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class SftpConnection
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("SftpAccount")]
        public Guid SftpAccountId { get; set; }
        public FileDrop SftpAccount { get; set; }
    }
}

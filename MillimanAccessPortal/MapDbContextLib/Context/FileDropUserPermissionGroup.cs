/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a FileDrop user group with specified permissions
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class FileDropUserPermissionGroup
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public bool ReadAccess { get; set; }

        [Required]
        public bool WriteAccess { get; set; }

        [Required]
        public bool DeleteAccess { get; set; }

        [Required]
        public bool IsPersonalGroup { get; set; }

        [ForeignKey("FileDrop")]
        public Guid FileDropId { get; set; }
        public FileDrop FileDrop { get; set; }

        public ICollection<SftpAccount> SftpAccounts { get; set; } = new List<SftpAccount>();
    }
}

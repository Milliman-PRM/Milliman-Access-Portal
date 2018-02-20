/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Represents a request to publish new content associated with a RootContentItem record
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Identity;

namespace MapDbContextLib.Context
{
    public class ContentPublicationRequest
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("RootContentItem")]
        public long RootContentItemId { get; set; }
        public RootContentItem RootContentItem { get; set; }

        [ForeignKey("ApplicationUser")]
        public long ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [Required]
        public string MasterFilePath { get; set; }

        [Column(TypeName = "jsonb")]
        public string ResultHierarchy { get; set; }

        [Required]
        // Default value is enforced in ApplicationDbContext.OnModelCreating()
        public DateTimeOffset CreateDateTime { get; set; }

    }
}

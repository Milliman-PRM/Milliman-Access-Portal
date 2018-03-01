/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Represents a reduction server task
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Models;
using MapDbContextLib.Identity;

namespace MapDbContextLib.Context
{
    public enum ReductionStatusEnum : long
    {
        Queued = 10,
        Reducing = 20,
        Reduced = 30,
        Pushed = 40,
        Canceled = 50,
        Replaced = 51,
    }

    public class ContentReductionTask
    {
        [Key]
        // Default value is enforced in ApplicationDbContext.OnModelCreating()
        public Guid Id { get; set; }

        [ForeignKey("ContentPublicationRequest")]
        public long? ContentPublicationRequestId { get; set; }
        public ContentPublicationRequest ContentPublicationRequest { get; set; }

        [ForeignKey("ApplicationUser")]
        public long ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [ForeignKey("SelectionGroup")]
        public long SelectionGroupId { get; set; }
        public SelectionGroup SelectionGroup { get; set; }

        [Required]
        // Default value is enforced in ApplicationDbContext.OnModelCreating()
        public DateTimeOffset CreateDateTime { get; set; }

        [Required]
        public ReductionStatusEnum ReductionStatus { get; set; }

        /// <summary>
        /// This path must be accessible to MAP application and reduction server.  
        /// May be different from master file in ContentPublicationRequest
        /// </summary>
        [Required]
        public string MasterFilePath { get; set; }

        /// <summary>
        /// null if reduction not requested.  Path must be accessible to MAP application and reduction server
        /// </summary>
        public string ResultFilePath { get; set; }

        [Column(TypeName ="jsonb")]
        public string SelectionCriteria { get; set; }
    }
}


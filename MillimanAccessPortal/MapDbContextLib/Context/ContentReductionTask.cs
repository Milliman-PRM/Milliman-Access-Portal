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
    /// <summary>
    /// Represents the evolving status of the task
    /// </summary>
    public enum ReductionStatusEnum : long
    {
        Unspecified = 0,    // Default unknown state
        Canceled = 1,       // The task was canceled before the reduction server began processing
        Discarded = 2,      // The task was completed by the reduction server, but a user did not publish the reduced document
        Replaced = 3,       // The reduced document was published, but a more recent document has since been published
        Validating = 11,    // The task is awaiting content validation (e.g. virus scan)
        Queued = 10,        // The task is in queue for reduction
        Reducing = 20,      // The reduction server is currently processing the reduction task
        Reduced = 30,       // The reduction server has completed the reduction task, but no user has pushed the reduced document
        Live = 40,          // The reduced document is published and is currently being served to users
        Error = 90,         // An error has occured
    }

    /// <summary>
    /// Represent what action MAP is requesting from the reduction server
    /// </summary>
    public enum TaskActionEnum
    {
        Unspecified = 0,    // Default unknown state
        HierarchyOnly = 1,
        HierarchyAndReduction = 2,
    }

    public class ContentReductionTask
    {
        // TODO: If all display names match enum values, then use .ToString() instead of a Dictionary.
        public static Dictionary<ReductionStatusEnum, string> ReductionStatusDisplayNames = new Dictionary<ReductionStatusEnum, string>
        {
            { ReductionStatusEnum.Unspecified, "Unspecified" },
            { ReductionStatusEnum.Canceled, "Canceled" },
            { ReductionStatusEnum.Discarded, "Discarded" },
            { ReductionStatusEnum.Replaced, "Replaced" },
            { ReductionStatusEnum.Validating, "Validating" },
            { ReductionStatusEnum.Queued, "Queued" },
            { ReductionStatusEnum.Reducing, "Reducing" },
            { ReductionStatusEnum.Reduced, "Reduced" },
            { ReductionStatusEnum.Live, "Live" },
            { ReductionStatusEnum.Error, "Error" },
        };

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
        /// This conveys a user level message regarding an error status from reduction server.
        /// </summary>
        public string ReductionStatusMessage { get; set; } = null;

        /// <summary>
        /// This path must be accessible to MAP application and reduction server.  
        /// May be different from master file in ContentPublicationRequest
        /// </summary>
        [Required]
        public string MasterFilePath { get; set; }

        /// <summary>
        /// null if reduction not requested.  Path must be accessible to MAP application and reduction server
        /// </summary>
        public string ResultFilePath { get; set; } = string.Empty;

        /// <summary>
        /// From reduction server. json is intended to deserialize to an instance of ContentReductionHierarchy
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string MasterContentHierarchy { get; set; }

        /// <summary>
        /// From reduction server. json is intended to deserialize to an instance of ContentReductionHierarchy
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string ReducedContentHierarchy { get; set; }

        [Column(TypeName ="jsonb")]
        public string SelectionCriteria { get; set; }

        public string MasterContentChecksum { get; set; }

        public string ReducedContentChecksum { get; set; }

        public TaskActionEnum TaskAction { get; set; } = TaskActionEnum.Unspecified;
    }
}


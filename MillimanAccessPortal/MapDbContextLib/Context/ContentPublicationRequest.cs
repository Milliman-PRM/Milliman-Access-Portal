/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Represents a request to publish new content associated with a RootContentItem record
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
using Newtonsoft.Json;

namespace MapDbContextLib.Context
{
    public enum PublicationStatus
    {
        Unknown = 0,
        Canceled = 1,
        Rejected = 2,
        Validating = 9,
        Queued = 10,
        Processing = 20,
        PostProcessReady = 25,
        PostProcessing = 27,
        Processed = 30,
        Confirming = 35,
        Confirmed = 40,
        Replaced = 50,
        Error = 90,         // An error has occured
    }

    public static class PublicationStatusExtensions
    {
        public readonly static List<PublicationStatus> ActiveStatuses = new List<PublicationStatus>
        {
            PublicationStatus.Validating,
            PublicationStatus.Queued,
            PublicationStatus.Processing,
            PublicationStatus.PostProcessReady,
            PublicationStatus.PostProcessing,
            PublicationStatus.Processed,
            PublicationStatus.Confirming,
        };

        public static bool IsCancelable(this PublicationStatus status)
        {
            var blockingStatuses = new List<PublicationStatus>
            {
                PublicationStatus.Validating,
                PublicationStatus.Queued,
            };

            return blockingStatuses.Contains(status);
        }

        public static bool IsActive(this PublicationStatus status)
        {

            return ActiveStatuses.Contains(status);
        }
    }


    public class ContentPublicationRequest
    {
        [NotMapped]
        public static Dictionary<PublicationStatus, string> PublicationStatusString = new Dictionary<PublicationStatus, string>
        {
            { PublicationStatus.Unknown, "Unknown" },
            { PublicationStatus.Canceled, "Canceled" },
            { PublicationStatus.Rejected, "Rejected"},
            { PublicationStatus.Validating, "Virus Scanning"},
            { PublicationStatus.Queued, "Queued"},
            { PublicationStatus.Processing, "Processing"},
            { PublicationStatus.PostProcessReady, "Processing"},
            { PublicationStatus.PostProcessing, "Post-Processing" },
            { PublicationStatus.Processed, "Processed (awaiting approval)"},
            { PublicationStatus.Confirming, "Going Live"},
            { PublicationStatus.Confirmed, "Confirmed"},
            { PublicationStatus.Replaced, "Replaced" },
            { PublicationStatus.Error, "Error" },
        };

        [Key]
        public Guid Id { get; set; }

        [ForeignKey("RootContentItem")]
        public Guid RootContentItemId { get; set; }
        public RootContentItem RootContentItem { get; set; }

        [ForeignKey("ApplicationUser")]
        public Guid ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [Column(TypeName = "jsonb")]
        public string ResultHierarchy { get; set; }

        [Required]
        // Default value is enforced in ApplicationDbContext.OnModelCreating()
        public DateTime CreateDateTimeUtc { get; set; }

        /// <summary>
        /// May also be accessed through [NotMapped] property LiveReadyFilesObj
        /// Intended to be serialization of type List<ContentRelatedFile>
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string LiveReadyFiles { get; set; } = "[]";

        /// <summary>
        /// May also be accessed through [NotMapped] property LiveReadyAssociatedFilesList
        /// Intended to be serialization of type List<ContentRelatedFile>
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string LiveReadyAssociatedFiles { get; set; } = "[]";

        /// <summary>
        /// May also be accessed through [NotMapped] property ReductionRelatedFilesObj
        /// Intended to be serialization of type List<ReductionRelatedFiles>
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string ReductionRelatedFiles { get; set; } = "[]";

        /// <summary>
        /// May also be accessed through [NotMapped] property UploadedRelatedFilesObj
        /// Intended to be serialization of type List<UploadedRelatedFile>
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string UploadedRelatedFiles { get; set; } = "[]";

        /// <summary>
        /// May also be accessed through [NotMapped] property RequestedAssociatedFilesList
        /// Intended to be serialization of type List<UploadedRelatedFile>
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string RequestedAssociatedFiles { get; set; } = "[]";

        [Required]
        public PublicationStatus RequestStatus { get; set; }

        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// May also be accessed through [NotMapped] property RequestMetadataObj
        /// Intended to be serialization of type RequestMetadata
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string OutcomeMetadata { get; set; } = "{}";

        /// <summary>
        /// Identifies files associated with work of the publishing server (input and output)
        /// </summary>
        [NotMapped]
        public List<ReductionRelatedFiles> ReductionRelatedFilesObj
        {
            get
            {
                return string.IsNullOrWhiteSpace(ReductionRelatedFiles)
                    ? new List<ReductionRelatedFiles>()
                    : JsonConvert.DeserializeObject<List<ReductionRelatedFiles>>(ReductionRelatedFiles);
            }
            set
            {
                ReductionRelatedFiles = JsonConvert.SerializeObject(value);
            }
        }

        /// <summary>
        /// Identifies files NOT associated with work of the publishing server, rather that are ready to switch to live status.
        /// </summary>
        [NotMapped]
        public List<ContentRelatedFile> LiveReadyFilesObj
        {
            get
            {
                return string.IsNullOrWhiteSpace(LiveReadyFiles)
                    ? new List<ContentRelatedFile>()
                    : JsonConvert.DeserializeObject<List<ContentRelatedFile>>(LiveReadyFiles);
            }
            set
            {
                LiveReadyFiles = value != null
                    ? JsonConvert.SerializeObject(value)
                    : "[]";
            }
        }

        /// <summary>
        /// Identifies content associated files NOT associated with work of the publishing server, rather that are ready to switch to live status.
        /// </summary>
        [NotMapped]
        public List<ContentAssociatedFile> LiveReadyAssociatedFilesList
        {
            get
            {
                return string.IsNullOrWhiteSpace(LiveReadyAssociatedFiles)
                    ? new List<ContentAssociatedFile>()
                    : JsonConvert.DeserializeObject<List<ContentAssociatedFile>>(LiveReadyAssociatedFiles);
            }
            set
            {
                LiveReadyAssociatedFiles = value != null
                    ? JsonConvert.SerializeObject(value)
                    : "[]";
            }
        }

        /// <summary>
        /// Identifies files uploaded as part of a publication request
        /// </summary>
        /// <remarks>This field is expected to be empty once uploaded files have been processed.</remarks>
        [NotMapped]
        public List<UploadedRelatedFile> UploadedRelatedFilesObj
        {
            get
            {
                return string.IsNullOrWhiteSpace(UploadedRelatedFiles)
                    ? new List<UploadedRelatedFile>()
                    : JsonConvert.DeserializeObject<List<UploadedRelatedFile>>(UploadedRelatedFiles);
            }
            set
            {
                UploadedRelatedFiles = value != null
                    ? JsonConvert.SerializeObject(value)
                    : "[]";
            }
        }

        /// <summary>
        /// The full list of associated files requested to exist upon completion of the publication go-live
        /// </summary>
        /// <remarks>This field is expected to be empty once uploaded files have been processed.</remarks>
        [NotMapped]
        public List<RequestedAssociatedFile> RequestedAssociatedFileList
        {
            get
            {
                return string.IsNullOrWhiteSpace(RequestedAssociatedFiles)
                    ? new List<RequestedAssociatedFile>()
                    : JsonConvert.DeserializeObject<List<RequestedAssociatedFile>>(RequestedAssociatedFiles);
            }
            set
            {
                RequestedAssociatedFiles = value != null
                    ? JsonConvert.SerializeObject(value)
                    : "[]";
            }
        }

        /// <summary>
        /// Identifies metadata about a publication request
        /// </summary>
        [NotMapped]
        public PublicationRequestOutcomeMetadata OutcomeMetadataObj
        {
            get
            {
                return string.IsNullOrWhiteSpace(OutcomeMetadata)
                    ? new PublicationRequestOutcomeMetadata { }
                    : JsonConvert.DeserializeObject<PublicationRequestOutcomeMetadata>(OutcomeMetadata);
            }
            set
            {
                OutcomeMetadata = value != null
                    ? JsonConvert.SerializeObject(value)
                    : "{}";
            }
        }
    }
}

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
        Error = 2,
        Queued = 10,
        Processing = 20,
        Processed = 30,
        Confirmed = 40,
    }

    public class ContentPublicationRequest
    {
        [NotMapped]
        public static Dictionary<PublicationStatus, string> PublicationStatusString = new Dictionary<PublicationStatus, string>
        {
            { PublicationStatus.Unknown, "Unknown" },
            { PublicationStatus.Canceled, "Canceled" },
            { PublicationStatus.Error, "Error" },
            { PublicationStatus.Queued, "Queued"},
            { PublicationStatus.Processing, "Processing"},
            { PublicationStatus.Processed, "Processed"},
            { PublicationStatus.Confirmed, "Confirmed"},
        };

        [Key]
        public long Id { get; set; }

        [ForeignKey("RootContentItem")]
        public long RootContentItemId { get; set; }
        public RootContentItem RootContentItem { get; set; }

        [ForeignKey("ApplicationUser")]
        public long ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [Column(TypeName = "jsonb")]
        public string ResultHierarchy { get; set; }

        [Required]
        // Default value is enforced in ApplicationDbContext.OnModelCreating()
        public DateTime CreateDateTimeUtc { get; set; }

        /// <summary>
        /// May also be accessed through [NotMapped] property PublishRequest
        /// Intended to be serialization of type MapDbContextLib.Models.UploadedContentRelatedFile[]
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string ContentRelatedFiles { get; set; } = "[]";

        /// <summary>
        /// Intended to be serialization of type MapDbContextLib.Models.ContentRelatedFile[]
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string ResultingContentFiles { get; set; } = "[]";

        [Required]
        public PublicationStatus RequestStatus { get; set; }

        public string StatusMessage { get; set; } = string.Empty;

        [NotMapped]
        public List<ContentRelatedFile> ResultingFiles
        {
            get
            {
                return JsonConvert.DeserializeObject<List<ContentRelatedFile>>(ResultingContentFiles);
            }
            set
            {
                ResultingContentFiles = JsonConvert.SerializeObject(value);
            }
        }

        [NotMapped]
        public List<ContentRelatedFile> RelatedInputFiles
        {
            get
            {
                return JsonConvert.DeserializeObject<List<ContentRelatedFile>>(ContentRelatedFiles);
            }
            set
            {
                ContentRelatedFiles = value != null
                    ? JsonConvert.SerializeObject(value)
                    : "[]";
            }
        }

        public static PublicationStatus GetPublicationStatus(List<ReductionStatusEnum> TaskStatusList)
        {
            List<ReductionStatusEnum> CompleteList = new List<ReductionStatusEnum> { ReductionStatusEnum.Reduced, ReductionStatusEnum.Canceled, ReductionStatusEnum.Live };
            List<ReductionStatusEnum> QueuedList = new List<ReductionStatusEnum> { ReductionStatusEnum.Queued };

            if (TaskStatusList.TrueForAll(s => CompleteList.Contains(s)))
            {
                return PublicationStatus.Processed;
            }

            else if (TaskStatusList.TrueForAll(s => s == ReductionStatusEnum.Queued))
            {
                return PublicationStatus.Queued;
            }

            else if (TaskStatusList.All(s => s == ReductionStatusEnum.Queued
                                          || s == ReductionStatusEnum.Reducing
                                          || s == ReductionStatusEnum.Reduced
                                          || s == ReductionStatusEnum.Replaced
                                          || s == ReductionStatusEnum.Canceled
                                          || s == ReductionStatusEnum.Discarded
                                          || s == ReductionStatusEnum.Live)
                  && TaskStatusList.Count(s => s == ReductionStatusEnum.Queued || s == ReductionStatusEnum.Reducing) > 0)
            {
                return PublicationStatus.Processing;
            }

            return PublicationStatus.Unknown;
        }
    }
}

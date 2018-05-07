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
        Queued = 10,
        Processing = 20,
        Complete = 30,
    }

    public class ContentPublicationRequest
    {
        [NotMapped]
        public static Dictionary<PublicationStatus, string> PublicationStatusString = new Dictionary<PublicationStatus, string>
        {
            { PublicationStatus.Unknown, "Unknown"},
            { PublicationStatus.Queued, "Queued"},
            { PublicationStatus.Processing, "Processing"},
            { PublicationStatus.Complete, "Complete"},
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
        public DateTimeOffset CreateDateTime { get; set; }

        /// <summary>
        /// May also be accessed through [NotMapped] property PublishRequest
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string ContentRelatedFiles { get; set; } = "{}";

        [NotMapped]
        public PublishRequest PublishRequest
        {
            get
            {
                return new PublishRequest
                {
                    RootContentItemId = RootContentItemId,
                    RelatedFiles = JsonConvert.DeserializeObject<ContentRelatedFile[]>(ContentRelatedFiles),
                };
            }
            set
            {
                RootContentItemId = value.RootContentItemId;
                ContentRelatedFiles = JsonConvert.SerializeObject(value.RelatedFiles);
            }
        }

        public static PublicationStatus GetPublicationStatus(List<ReductionStatusEnum> TaskStatusList)
        {
            List<ReductionStatusEnum> CompleteList = new List<ReductionStatusEnum> { ReductionStatusEnum.Reduced, ReductionStatusEnum.Canceled, ReductionStatusEnum.Live };
            List<ReductionStatusEnum> QueuedList = new List<ReductionStatusEnum> { ReductionStatusEnum.Queued };

            if (TaskStatusList.TrueForAll(s => CompleteList.Contains(s)))
            {
                return PublicationStatus.Complete;
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

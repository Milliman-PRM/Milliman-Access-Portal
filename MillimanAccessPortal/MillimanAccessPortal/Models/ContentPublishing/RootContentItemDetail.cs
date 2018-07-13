/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: Model root content item details available to users
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemDetail
    {
        public long Id { get; set; }

        [HiddenInput]
        public long ClientId { get; set; }

        [Required]
        [Display(Name = "Content Name")]
        public string ContentName { get; set; }

        [Range(1, Int64.MaxValue, ErrorMessage = "You must select a content type")]
        [Display(Name = "Content Type")]
        public long ContentTypeId { get; set; }

        [Display(Name = "Does Reduce")]
        public bool DoesReduce { get; set; }

        public List<ContentRelatedFile> RelatedFiles { get; set; }

        public string Description { get; set; }

        public string Notes { get; set; }

        internal static RootContentItemDetail Build(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            var publicationRequest = dbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == rootContentItem.Id)
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefault();
            var activeStatuses = new List<PublicationStatus>
            {
                PublicationStatus.Queued,
                PublicationStatus.Processing,
                PublicationStatus.Processed,
            };

            List<ContentRelatedFile> relatedFiles = rootContentItem.ContentFilesList;
            if (activeStatuses.Contains(publicationRequest?.RequestStatus ?? PublicationStatus.Unknown))
            {
                var oldFiles = rootContentItem.ContentFilesList;
                var newFiles = publicationRequest.LiveReadyFilesObj;
                newFiles.AddRange(oldFiles.Where(f => !newFiles.Select(n => n.FilePurpose).Contains(f.FilePurpose)));
                relatedFiles = newFiles;
            }

            var model = new RootContentItemDetail
            {
                Id = rootContentItem.Id,
                ClientId = rootContentItem.ClientId,
                ContentName = rootContentItem.ContentName,
                ContentTypeId = rootContentItem.ContentTypeId,
                DoesReduce = rootContentItem.DoesReduce,
                RelatedFiles = relatedFiles,
                Description = rootContentItem.Description,
                Notes = rootContentItem.Notes,
            };

            return model;
        }
    }
}

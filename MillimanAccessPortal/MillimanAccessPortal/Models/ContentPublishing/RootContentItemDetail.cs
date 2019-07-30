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
        [HiddenInput]
        public Guid ClientId { get; set; }

        public string ContentDisclaimer { get; set; }

        [Required]
        [Display(Name = "Content Name *")]
        public string ContentName { get; set; }

        [Display(Name = "Content Type *")]
        public Guid ContentTypeId { get; set; }

        public string ContentDescription { get; set; }

        [Display(Name = "Does Reduce")]
        public bool DoesReduce { get; set; }

        public Guid Id { get; set; }

        public bool IsSuspended { get; set; }

        public string ContentNotes { get; set; }

        public Dictionary<string, ContentRelatedFile> RelatedFiles { get; set; } = new Dictionary<string, ContentRelatedFile>();

        public List<ContentAssociatedFile> AssociatedFiles { get; set; }

        public TypeSpecificContentItemProperties TypeSpecificDetailObject { get; set; }

        internal static RootContentItemDetail Build(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            var publicationRequest = dbContext.ContentPublicationRequest
                .Where(r => r.RootContentItemId == rootContentItem.Id)
                .OrderByDescending(r => r.CreateDateTimeUtc)
                .FirstOrDefault();
            var contentType = dbContext.ContentType.Find(rootContentItem.ContentTypeId);

            List<ContentRelatedFile> relatedFiles = rootContentItem.ContentFilesList;
            if ((publicationRequest?.RequestStatus ?? PublicationStatus.Unknown).IsActive())
            {
                var oldFiles = rootContentItem.ContentFilesList;
                var newFiles = publicationRequest.UploadedRelatedFilesObj.Any()
                    ? publicationRequest.UploadedRelatedFilesObj.Select(f => new ContentRelatedFile
                    {
                        FileOriginalName = f.FileOriginalName,
                        FilePurpose = f.FilePurpose,
                    }).ToList()
                    : publicationRequest.LiveReadyFilesObj;
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
                RelatedFiles = relatedFiles.ToDictionary(f => f.FilePurpose),
                AssociatedFiles = rootContentItem.AssociatedFilesList,
                ContentDescription = rootContentItem.Description,
                ContentNotes = rootContentItem.Notes,
                ContentDisclaimer = rootContentItem.ContentDisclaimer,
                IsSuspended = rootContentItem.IsSuspended,
                TypeSpecificDetailObject = default,
            };
            switch (contentType.TypeEnum)
            {
                case ContentTypeEnum.PowerBi:
                    model.TypeSpecificDetailObject = rootContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties;
                    break;
                case ContentTypeEnum.Qlikview:
                case ContentTypeEnum.Pdf:
                case ContentTypeEnum.Html:
                case ContentTypeEnum.FileDownload:
                default:
                    break;
            }

            return model;
        }
    }
}

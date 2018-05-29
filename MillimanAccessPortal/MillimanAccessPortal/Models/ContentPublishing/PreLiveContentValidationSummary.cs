using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Models;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PreLiveContentValidationSummary
    {
        public string RootContentName { get; set; }
        public string ContentTypeName { get; set; }
        public string ContentDescription { get; set; }
        public bool DoesReduce { get; set; }
        public string ClientName { get; set; }
        public string ClientCode { get; set; }
        public string AttestationLanguage { get; set; }
        public string MasterContentLink { get; set; }
        public string UserGuideLink { get; set; }
        public string ReleaseNotesLink { get; set; }
        public string ThumbnailLink { get; set; }
        public ContentReductionHierarchy<ReductionFieldValue> OldHierarchy { get; set; }
        public ContentReductionHierarchy<ReductionFieldValue> NewHierarchy { get; set; }
        public SelectionGroupSummary[] SelectionGroups { get; set; }

        public static PreLiveContentValidationSummary Build(ApplicationDbContext Db, long RootContentItemId)
        {
            // Two separate queries because PubRequest and its navigation properties are single records, Tasks and SelectionGroups are multiple
            ContentPublicationRequest PubRequest = Db.ContentPublicationRequest
                                                     .Include(r => r.RootContentItem).ThenInclude(c => c.ContentType)
                                                     .Include(r => r.RootContentItem).ThenInclude(c => c.Client)
                                                     .SingleOrDefault(r => r.RootContentItemId == RootContentItemId);
            #region Validation of PubRequest and nav properties from db
            if (PubRequest == null
             || PubRequest.RootContentItem == null
             || PubRequest.RootContentItem.ContentType == null
             || PubRequest.RootContentItem.Client == null
             )  // if any of this happens it probably means db corruption or connection failed
            {
                throw new ApplicationException($"While building content validation summary, publication request query failed");
            }
            #endregion

            List<ContentReductionTask> AllTasks = Db.ContentReductionTask
                                                    .Include(t => t.SelectionGroup)
                                                    .Where(t => t.ContentPublicationRequestId == PubRequest.Id)
                                                    .ToList();

            #region Validation of reduction tasks and nav properties from db
            if (AllTasks.Count == 0 
             || AllTasks.Any(t => t.SelectionGroup == null)
             || AllTasks.Any(t => t.SelectionGroup.RootContentItemId != PubRequest.RootContentItemId)
             )  // if any of this happens it probably means db corruption or connection failed
            {
                throw new ApplicationException($"While building content validation summary, reduction task query failed");
            }
            #endregion

            PreLiveContentValidationSummary ReturnObj = new PreLiveContentValidationSummary
            {
                RootContentName = PubRequest.RootContentItem.ContentName,
                ContentTypeName = PubRequest.RootContentItem.ContentType.Name,
                ContentDescription = PubRequest.RootContentItem.Description,
                DoesReduce = PubRequest.RootContentItem.DoesReduce,
                ClientName = PubRequest.RootContentItem.Client.Name,
                ClientCode = PubRequest.RootContentItem.Client.ClientCode,
                AttestationLanguage = string.Empty,
                MasterContentLink = string.Empty,
                UserGuideLink = string.Empty,
                ReleaseNotesLink = string.Empty,
                ThumbnailLink = string.Empty,
                OldHierarchy = new ContentReductionHierarchy<ReductionFieldValue>(),
                NewHierarchy = new ContentReductionHierarchy<ReductionFieldValue>(),
                SelectionGroups = new SelectionGroupSummary[0],
            };

            // TODO populate the link and object members

            return ReturnObj;
        }
    }

    public class SelectionGroupSummary
    {
        public string Name { get; set; }
        public int UserCount { get; set; }
        public bool IsMaster { get; set; }
    }
}

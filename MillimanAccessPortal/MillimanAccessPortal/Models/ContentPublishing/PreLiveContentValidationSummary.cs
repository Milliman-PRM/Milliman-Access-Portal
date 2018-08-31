using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Models;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QlikviewLib;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PreLiveContentValidationSummary
    {
        public string ValidationSummaryId { get; set; }
        public Guid PublicationRequestId { get; set; }
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
        public ContentReductionHierarchy<ReductionFieldValue> LiveHierarchy { get; set; }
        public ContentReductionHierarchy<ReductionFieldValue> NewHierarchy { get; set; }
        public List<SelectionGroupSummary> SelectionGroups { get; set; }

        public static async Task<PreLiveContentValidationSummary> Build(ApplicationDbContext Db, Guid RootContentItemId, IConfiguration ApplicationConfig, string UserName, object ContentTypeConfig)
        {
            ContentPublicationRequest PubRequest = Db.ContentPublicationRequest
                                                     .Include(r => r.RootContentItem).ThenInclude(c => c.ContentType)
                                                     .Include(r => r.RootContentItem).ThenInclude(c => c.Client)
                                                     .Where(r => r.RootContentItemId == RootContentItemId)
                                                     .SingleOrDefault(r => r.RequestStatus == PublicationStatus.Processed);
            #region Validation of PubRequest and related nav properties from db
            if (PubRequest == null
             || PubRequest.RootContentItem == null
             || PubRequest.RootContentItem.ContentType == null
             || PubRequest.RootContentItem.Client == null
             )  // if any of this happens it probably means db corruption or connection failed
            {
                throw new ApplicationException($"While building content validation summary, publication request query failed");
            }
            #endregion

            PreLiveContentValidationSummary ReturnObj = new PreLiveContentValidationSummary
            {
                ValidationSummaryId = Guid.NewGuid().ToString("D"),
                PublicationRequestId = PubRequest.Id,
                RootContentName = PubRequest.RootContentItem.ContentName,
                ContentTypeName = PubRequest.RootContentItem.ContentType.Name,
                ContentDescription = PubRequest.RootContentItem.Description,
                DoesReduce = PubRequest.RootContentItem.DoesReduce,
                ClientName = PubRequest.RootContentItem.Client.Name,
                ClientCode = PubRequest.RootContentItem.Client.ClientCode,
                AttestationLanguage = ApplicationConfig.GetValue<string>("Publishing:AttestationLanguage"),
                MasterContentLink = null,
                UserGuideLink = null,
                ReleaseNotesLink = null,
                ThumbnailLink = null,
            };

            if (PubRequest.RootContentItem.DoesReduce)
            {
                List<ContentReductionTask> AllTasks = Db.ContentReductionTask
                                                        .Include(t => t.SelectionGroup)
                                                        .Where(t => t.ContentPublicationRequestId == PubRequest.Id)
                                                        .ToList();
                #region Validation of reduction tasks and related nav properties from db
                if (AllTasks.Any(t => t.SelectionGroup == null)
                 || AllTasks.Any(t => t.SelectionGroup.RootContentItemId != PubRequest.RootContentItemId)
                 )  // if any of this happens it probably means db corruption or connection failed
                {
                    throw new ApplicationException($"While building content validation summary, reduction task query failed");
                }
                #endregion

                ReturnObj.LiveHierarchy = ContentReductionHierarchy<ReductionFieldValue>.GetHierarchyForRootContentItem(Db, RootContentItemId);
                ReturnObj.NewHierarchy = AllTasks.Any() ? AllTasks[0].MasterContentHierarchyObj : null;  // null == there was no hierarchy extraction
                ReturnObj.SelectionGroups = AllTasks.Select(t => new SelectionGroupSummary
                    {
                        Name = t.SelectionGroup.GroupName,
                        IsMaster = t.SelectionGroup.IsMaster,
                        UserCount = Db.UserInSelectionGroup.Count(usg => usg.SelectionGroupId == t.SelectionGroup.Id),
                    }
                ).ToList();
            }
            else
            {
                ReturnObj.LiveHierarchy = null;
                ReturnObj.NewHierarchy = null;
                ReturnObj.SelectionGroups = null;
            }

            string ContentRootPath = ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath");            
            foreach (ContentRelatedFile RelatedFile in PubRequest.LiveReadyFilesObj)
            {
                string Link = Path.GetRelativePath(ContentRootPath, RelatedFile.FullPath);
                switch (RelatedFile.FilePurpose.ToLower())
                {
                    case "mastercontent":
                        switch (PubRequest.RootContentItem.ContentType.TypeEnum)
                        {
                            case ContentTypeEnum.Qlikview:
                                // TODO authorize this document.  This is not the production file name.
                                await new QlikviewLibApi().AuthorizeUserDocumentsInFolder(Path.GetDirectoryName(Link), ContentTypeConfig as QlikviewConfig, Path.GetFileName(Link));

                                UriBuilder QvwUri = await new QlikviewLibApi().GetContentUri(Link, UserName, ContentTypeConfig);
                                ReturnObj.MasterContentLink = QvwUri.Uri.AbsoluteUri;
                                break;

                            default:
                                break;
                        }
                        break;

                    case "thumbnail":
                        ReturnObj.ThumbnailLink = Link;
                        break;

                    case "userguide":
                        ReturnObj.UserGuideLink = Link;
                        break;

                    case "releasenotes":
                        ReturnObj.ReleaseNotesLink = Link;
                        break;
                }
            }

            return ReturnObj;
        }
    }

    public class SelectionGroupSummary
    {
        public string Name { get; set; } = string.Empty;
        public int UserCount { get; set; } = 0;
        public bool IsMaster { get; set; }
    }
}

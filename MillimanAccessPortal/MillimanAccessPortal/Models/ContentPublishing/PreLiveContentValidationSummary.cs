/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: View model for the content preview/approval form
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Models;
using MapDbContextLib.Models;
using MapDbContextLib.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MillimanAccessPortal.Controllers;
using MillimanAccessPortal.Models.AccountViewModels;
using Serilog;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PreLiveContentValidationSummary
    {
        public string ValidationSummaryId { get; set; }
        public Guid PublicationRequestId { get; set; }
        public Guid RootContentId { get; set; }
        public string RootContentName { get; set; }
        public string ContentTypeName { get; set; }
        public string ContentDescription { get; set; }
        public bool DoesReduce { get; set; }
        public Guid ClientId { get; set; }
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
        public List<AssociatedFileSummary> AssociatedFiles { get; set; } = new List<AssociatedFileSummary>();

        public static PreLiveContentValidationSummary Build(ApplicationDbContext Db, Guid RootContentItemId, IConfiguration ApplicationConfig, HttpContext Context)
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
                RootContentId = PubRequest.RootContentItem.Id,
                RootContentName = PubRequest.RootContentItem.ContentName,
                ContentTypeName = PubRequest.RootContentItem.ContentType.Name,
                ContentDescription = PubRequest.RootContentItem.Description,
                DoesReduce = PubRequest.RootContentItem.DoesReduce,
                ClientId = PubRequest.RootContentItem.Client.Id,
                ClientName = PubRequest.RootContentItem.Client.Name,
                ClientCode = PubRequest.RootContentItem.Client.ClientCode,
                AttestationLanguage = ApplicationConfig.GetValue<string>("Publishing:AttestationLanguage"),
                MasterContentLink = null,
                UserGuideLink = null,
                ReleaseNotesLink = null,
                ThumbnailLink = null,
            };

            ReturnObj.LiveHierarchy = null;
            ReturnObj.NewHierarchy = null;
            ReturnObj.SelectionGroups = null;
            if (PubRequest.RootContentItem.DoesReduce)
            {
                // retrieve all reduction tasks for this publication, filtering out the request
                // responsible for extracting the new hierarchy
                List<ContentReductionTask> AllTasks = Db.ContentReductionTask
                                                        .Include(t => t.SelectionGroup)
                                                        .Where(t => t.ContentPublicationRequestId == PubRequest.Id)
                                                        .Where(t => t.SelectionGroup != null)
                                                        .ToList();
                #region Validation of reduction tasks and related nav properties from db
                if (AllTasks.Any(t => t.SelectionGroup == null)
                 || AllTasks.Any(t => t.SelectionGroup.RootContentItemId != PubRequest.RootContentItemId)
                 )  // if any of this happens it probably means db corruption or connection failed
                {
                    throw new ApplicationException($"While building content validation summary, reduction task query failed");
                }
                #endregion

                var newHierarchy = AllTasks.FirstOrDefault()?.MasterContentHierarchyObj;

                if (newHierarchy != null)
                {
                    newHierarchy.Sort();

                    var selectionGroups = new List<SelectionGroupSummary>();
                    foreach (var task in AllTasks)
                    {
                        var selectionGroupUsers = new List<UserInfoViewModel>();
                        var userQuery = Db.UserInSelectionGroup
                            .Where(usg => usg.SelectionGroupId == task.SelectionGroup.Id)
                            .Select(usg => usg.User);
                        foreach (var user in userQuery)
                        {
                            var userInfo = (UserInfoViewModel)user;
                            selectionGroupUsers.Add(userInfo); 
                        }

                        string errorMessage;
                        switch (task.OutcomeMetadataObj.OutcomeReason)
                        {
                            case MapDbReductionTaskOutcomeReason.NoSelectedFieldValues:
                                errorMessage = "This group has no selections.";
                                break;
                            case MapDbReductionTaskOutcomeReason.NoSelectedFieldValueExistsInNewContent:
                                errorMessage = "None of this group's selections are in the new hierarchy.";
                                break;
                            case MapDbReductionTaskOutcomeReason.NoReducedFileCreated:
                                errorMessage = "The reduction did not produce an output file. "
                                    + "This could be caused by selections that result in no matching data.";
                                break;
                            default:
                                errorMessage = null;
                                Log.Warning("Unexpected outcome reason in go live preview "
                                    + $"for reduction task {task.Id}: {task.OutcomeMetadataObj.OutcomeReason}");
                                break;
                        }

                        selectionGroups.Add(new SelectionGroupSummary
                        {
                            Id = task.SelectionGroup.Id,
                            Name = task.SelectionGroup.GroupName,
                            IsMaster = task.SelectionGroup.IsMaster,
                            Duration = task.OutcomeMetadataObj.ElapsedTime,
                            Users = selectionGroupUsers,
                            WasInactive = task.SelectionGroup.ContentInstanceUrl == null,
                            IsInactive = task.ReductionStatus != ReductionStatusEnum.Reduced,
                            InactiveReason = errorMessage,
                            LiveSelections = task.SelectionGroup.IsMaster
                                ? null
                                : ContentReductionHierarchy<ReductionFieldValueSelection>
                                    .GetFieldSelectionsForSelectionGroup(Db, task.SelectionGroupId.Value),
                            PendingSelections = task.SelectionGroup.IsMaster
                                ? null
                                : ContentReductionHierarchy<ReductionFieldValueSelection>.Apply(
                                    task.MasterContentHierarchyObj, task.SelectionCriteriaObj),
                        });
                    }

                    ReturnObj.LiveHierarchy = ContentReductionHierarchy<ReductionFieldValue>.GetHierarchyForRootContentItem(Db, RootContentItemId);
                    ReturnObj.NewHierarchy = newHierarchy;
                    ReturnObj.SelectionGroups = selectionGroups;
                }
            }

            foreach (var associatedFile in PubRequest.LiveReadyAssociatedFilesList)
            {
                var summary = new AssociatedFileSummary
                {
                    Id = associatedFile.Id,
                    DisplayName = associatedFile.DisplayName,
                    FileOriginalName = associatedFile.FileOriginalName,
                    FileType = associatedFile.FileType,
                    SortOrder = associatedFile.SortOrder,
                };
                UriBuilder builder = new UriBuilder
                {
                    Scheme = Context.Request.Scheme,
                    Host = Context.Request.Host.Host,
                    Port = Context.Request.Host.Port ?? -1,
                    Path = $"/AuthorizedContent/{nameof(AuthorizedContentController.AssociatedFilePreview)}",
                    Query = $"publicationRequestId={PubRequest.Id}&fileId={associatedFile.Id}",
                };
                summary.Link = builder.Uri.AbsoluteUri;
                ReturnObj.AssociatedFiles.Add(summary);
            }

            string ContentRootPath = ApplicationConfig.GetValue<string>("Storage:ContentItemRootPath");
            foreach (ContentRelatedFile RelatedFile in PubRequest.LiveReadyFilesObj)
            {
                string Link = Path.GetRelativePath(ContentRootPath, RelatedFile.FullPath);
                UriBuilder contentUri = new UriBuilder
                {
                    Scheme = Context.Request.Scheme,
                    Host = Context.Request.Host.Host ?? "localhost",  // localhost is probably error in production but won't crash
                    Port = Context.Request.Host.Port ?? -1,
                };

                switch (RelatedFile.FilePurpose.ToLower())
                {
                    case "mastercontent":
                        switch (PubRequest.RootContentItem.ContentType.TypeEnum)
                        {
                            case ContentTypeEnum.PowerBi:
                                contentUri.Path = $"/AuthorizedContent/{nameof(AuthorizedContentController.PowerBiPreview)}";
                                contentUri.Query = $"request={PubRequest.Id}";

                                ReturnObj.MasterContentLink = contentUri.Uri.AbsoluteUri;
                                break;

                            case ContentTypeEnum.Qlikview:
                                contentUri.Path = "/AuthorizedContent/QvwPreview";
                                contentUri.Query = $"publicationRequestId={PubRequest.Id}";
                                ReturnObj.MasterContentLink = contentUri.Uri.AbsoluteUri;
                                break;

                            case ContentTypeEnum.Pdf:
                                contentUri.Path = "/AuthorizedContent/PdfPreview";
                                contentUri.Query = $"purpose=mastercontent&publicationRequestId={PubRequest.Id}";
                                ReturnObj.MasterContentLink = contentUri.Uri.AbsoluteUri;
                                break;

                            case ContentTypeEnum.Html:
                                contentUri.Path = "/AuthorizedContent/HtmlPreview";
                                contentUri.Query = $"publicationRequestId={PubRequest.Id}";
                                ReturnObj.MasterContentLink = contentUri.Uri.AbsoluteUri;
                                break;

                            case ContentTypeEnum.FileDownload:
                                contentUri.Path = "/AuthorizedContent/FileDownloadPreview";
                                contentUri.Query = $"publicationRequestId={PubRequest.Id}";
                                ReturnObj.MasterContentLink = contentUri.Uri.AbsoluteUri;
                                break;

                            default:
                                break;
                        }
                        break;

                    case "thumbnail":
                        contentUri.Path = "/AuthorizedContent/ThumbnailPreview";
                        contentUri.Query = $"publicationRequestId={PubRequest.Id}";
                        // this doesn't happen
                        ReturnObj.ThumbnailLink = contentUri.Uri.AbsoluteUri;
                        break;

                    case "userguide":
                        contentUri.Path = "/AuthorizedContent/PdfPreview";
                        contentUri.Query = $"purpose=userguide&publicationRequestId={PubRequest.Id}";
                        ReturnObj.UserGuideLink = contentUri.Uri.AbsoluteUri;
                        break;

                    case "releasenotes":
                        contentUri.Path = "/AuthorizedContent/PdfPreview";
                        contentUri.Query = $"purpose=releasenotes&publicationRequestId={PubRequest.Id}";
                        ReturnObj.ReleaseNotesLink = contentUri.Uri.AbsoluteUri;
                        break;
                }
            }

            return ReturnObj;
        }

        public static explicit operator PreLiveContentValidationSummaryLogModel(PreLiveContentValidationSummary source)
        {
            return new PreLiveContentValidationSummaryLogModel
            {
                ValidationSummaryId = source.ValidationSummaryId,
                PublicationRequestId = source.PublicationRequestId,
                AttestationLanguage = source.AttestationLanguage,
                ContentDescription = source.ContentDescription,
                RootContentId = source.RootContentId,
                RootContentName = source.RootContentName,
                ContentTypeName = source.ContentTypeName,
                LiveHierarchy = source.LiveHierarchy,
                NewHierarchy = source.NewHierarchy,
                DoesReduce = source.DoesReduce,
                ClientId = source.ClientId,
                ClientName = source.ClientName,
            };
        }
    }

    public class SelectionGroupSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsMaster { get; set; }
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public List<UserInfoViewModel> Users { get; set; } = new List<UserInfoViewModel>();
        public bool WasInactive { get; set; }
        public bool IsInactive { get; set; }
        public string InactiveReason { get; set; } = null;
        public ContentReductionHierarchy<ReductionFieldValueSelection> LiveSelections { get; set; }
        public ContentReductionHierarchy<ReductionFieldValueSelection> PendingSelections { get; set; }
    }

    public class AssociatedFileSummary
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string FileOriginalName { get; set; }
        public string SortOrder { get; set; } = null;
        public ContentAssociatedFileType FileType { get; set; } = ContentAssociatedFileType.Unknown;
        public string Link { get; set; } = string.Empty;

        public static explicit operator AssociatedFileSummary(ContentAssociatedFile source)
        {
            return new AssociatedFileSummary
            {
                Id = source.Id,
                DisplayName = source.DisplayName,
                FileOriginalName = source.FileOriginalName,
                FileType = source.FileType,
                SortOrder = source.SortOrder,
                Link = string.Empty,
            };
        }
    }
}

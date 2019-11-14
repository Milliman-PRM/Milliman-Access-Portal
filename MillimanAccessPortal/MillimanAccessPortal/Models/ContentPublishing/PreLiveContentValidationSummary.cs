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
        public ContentReductionHierarchy<ReductionFieldValueChange> ReductionHierarchy { get; set; }
        public List<SelectionGroupSummary> SelectionGroups { get; set; } = null;
        public List<AssociatedFilePreviewSummary> AssociatedFiles { get; set; } = new List<AssociatedFilePreviewSummary>();

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

                    ReturnObj.SelectionGroups = new List<SelectionGroupSummary>();
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

                        var liveSelections = task.SelectionGroup.IsMaster
                            ? null
                            : ContentReductionHierarchy<ReductionFieldValueSelection>.GetFieldSelectionsForSelectionGroup(Db, task.SelectionGroupId.Value);
                        var pendingSelections = task.SelectionGroup.IsMaster
                            ? null
                            : ContentReductionHierarchy<ReductionFieldValueSelection>.Apply(task.MasterContentHierarchyObj, task.SelectionCriteriaObj);

                        ReturnObj.SelectionGroups.Add(new SelectionGroupSummary
                        {
                            Id = task.SelectionGroup.Id,
                            Name = task.SelectionGroup.GroupName,
                            IsMaster = task.SelectionGroup.IsMaster,
                            Duration = task.OutcomeMetadataObj.ElapsedTime,
                            Users = selectionGroupUsers,
                            WasInactive = task.SelectionGroup.ContentInstanceUrl == null,
                            IsInactive = task.ReductionStatus != ReductionStatusEnum.Reduced,
                            InactiveReason = errorMessage,
                            SelectionChanges = task.SelectionGroup.IsMaster
                                ? null
                                : new ContentReductionHierarchy<ReductionFieldValueChange>
                                {
                                    RootContentItemId = RootContentItemId,
                                    Fields = task.SelectionCriteriaObj.Fields.Select(f =>
                                    {
                                        var addedValues = pendingSelections.Fields.Single(fp => fp.FieldName == f.FieldName).Values.Where(vp => vp.SelectionStatus).Except(
                                            liveSelections.Fields.Single(fl => fl.FieldName == f.FieldName).Values.Where(vl => vl.SelectionStatus), new ReductionFieldValueComparer());

                                        var removedValues = liveSelections.Fields.Single(fl => fl.FieldName == f.FieldName).Values.Where(vl => vl.SelectionStatus).Except(
                                            pendingSelections.Fields.Single(fp => fp.FieldName == f.FieldName).Values.Where(vp => vp.SelectionStatus), new ReductionFieldValueComparer());

                                        var unchangedValues = liveSelections.Fields.Single(fl => fl.FieldName == f.FieldName).Values.Where(vl => vl.SelectionStatus).Intersect(
                                            pendingSelections.Fields.Single(fp => fp.FieldName == f.FieldName).Values.Where(vp => vp.SelectionStatus), new ReductionFieldValueComparer());

                                        return new ReductionField<ReductionFieldValueChange>
                                        {
                                            FieldName = f.FieldName,
                                            DisplayName = f.DisplayName,
                                            Id = f.Id,
                                            StructureType = f.StructureType,
                                            ValueDelimiter = f.ValueDelimiter,
                                            Values = addedValues.Select(v => new ReductionFieldValueChange
                                            {
                                                Value = v.Value,
                                                Id = v.Id,
                                                ValueChange = FieldValueChange.Added,
                                            })
                                            .Concat(removedValues.Select(v => new ReductionFieldValueChange
                                            {
                                                Value = v.Value,
                                                Id = v.Id,
                                                ValueChange = FieldValueChange.Removed,
                                            }))
                                            .Concat(unchangedValues.Select(v => new ReductionFieldValueChange
                                            {
                                                Value = v.Value,
                                                Id = v.Id,
                                                ValueChange = FieldValueChange.NoChange,
                                            }))
                                            .OrderBy(v => v.Value)
                                            .ToList()
                                        };
                                    })
                                    .ToList(),
                                }
                        });
                    }

                    ReturnObj.ReductionHierarchy = new ContentReductionHierarchy<ReductionFieldValueChange> { RootContentItemId = RootContentItemId };
                    var liveHierarchy = ContentReductionHierarchy<ReductionFieldValue>.GetHierarchyForRootContentItem(Db, RootContentItemId);

                    foreach (var field in newHierarchy.Fields)
                    {
                        var addedValues = field.Values.Except(liveHierarchy.Fields.Single(f => f.FieldName == field.FieldName).Values, new ReductionFieldValueComparer());
                        var removedValues = liveHierarchy.Fields.Single(f => f.FieldName == field.FieldName).Values.Except(field.Values, new ReductionFieldValueComparer());
                        var sameValues = liveHierarchy.Fields.Single(f => f.FieldName == field.FieldName).Values.Intersect(field.Values, new ReductionFieldValueComparer());

                        ReturnObj.ReductionHierarchy.Fields.Add(new ReductionField<ReductionFieldValueChange>
                            {
                                Id = field.Id,
                                DisplayName = field.DisplayName,
                                FieldName = field.FieldName,
                                StructureType = field.StructureType,
                                ValueDelimiter = field.ValueDelimiter,
                                Values = addedValues.Select(v => new ReductionFieldValueChange
                                    {
                                        Id = v.Id,
                                        Value = v.Value,
                                        ValueChange = FieldValueChange.Added,
                                    })
                                    .Concat(removedValues.Select(v => new ReductionFieldValueChange
                                    {
                                        Id = v.Id,
                                        Value = v.Value,
                                        ValueChange = FieldValueChange.Removed,
                                    }))
                                    .Concat(sameValues.Select(v => new ReductionFieldValueChange
                                    {
                                        Id = v.Id,
                                        Value = v.Value,
                                        ValueChange = FieldValueChange.NoChange,
                                    }))
                                    .OrderBy(v => v.Value)
                                    .ToList(),
                            });
                    }
                }
            }

            foreach (var associatedFile in PubRequest.LiveReadyAssociatedFilesList)
            {
                var summary = new AssociatedFilePreviewSummary(associatedFile);
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
                HierarchyComparison = source.ReductionHierarchy,
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
        public ContentReductionHierarchy<ReductionFieldValueChange> SelectionChanges { get; set; }
    }

    public class AssociatedFilePreviewSummary : AssociatedFileModel
    {
        public string Link { get; set; } = string.Empty;

        public AssociatedFilePreviewSummary(ContentAssociatedFile source)
            : base(source)
        {
            Link = string.Empty;
        }
    }

}

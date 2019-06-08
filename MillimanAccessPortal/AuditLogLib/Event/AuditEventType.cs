/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Models;
using MapDbContextLib.Models;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AuditLogLib.Event
{
    public sealed class AuditEventType : AuditEventTypeBase
    {
        #region Static event type declarations
        // WARNING!!!  After production begins, never change the numeric ID of any AuditEventType

        #region Uncategorized [0000 - 0999]
        public static readonly AuditEventType Unspecified = new AuditEventType(0001, "Unspecified");
        public static readonly AuditEventType InvalidRequest = new AuditEventType(0002, "Invalid request");
        #endregion

        #region User activity [1000 - 1999]
        public static readonly AuditEventType<string> LoginSuccess = new AuditEventType<string>(
            1001, "Login success", (scheme) => new
            {
                Scheme = scheme,
            });
        public static readonly AuditEventType<string, string> LoginFailure = new AuditEventType<string, string>(
            1002, "Login failure", (attemptedUsername, scheme) => new
            {
                AttemptedUsername = attemptedUsername,
                AuthenticationScheme = scheme,
            });
        public static readonly AuditEventType<RoleEnum> Unauthorized = new AuditEventType<RoleEnum>(
            1003, "Unauthorized request", (role) => new
            {
                Role = role.ToString(),
            });
        public static readonly AuditEventType Logout = new AuditEventType(1004, "Logout success");
        public static readonly AuditEventType AccountLockByUser = new AuditEventType(1005, "Account lock by user");
        public static readonly AuditEventType UserPasswordChanged = new AuditEventType(1006, "User password changed");
        public static readonly AuditEventType<string, string, string, string, int> ManualDatabaseCommand =
            new AuditEventType<string, string, string, string, int>(1007, "Manual database command",
            (userName, githubUrl, approverName, queryText, rows) => new
            {
                UserName = userName,
                GitHubIssue = githubUrl,
                Approver = approverName,
                QueryText = queryText,
                RowsAffected = rows,
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client> UserContentAccess =
           new AuditEventType<SelectionGroup, RootContentItem, Client>(1008, "Content access",
               (selectionGroup, contentItem, client) => new
               {
                   SelectionGroup = new
                   {
                       Id = selectionGroup.Id,
                       GroupName = selectionGroup.GroupName,
                   },
                   ContentItem = new
                   {
                       Id = contentItem.Id,
                       ContentName = contentItem.ContentName,
                   },
                   Client = new
                   {
                       Id = client.Id,
                       Name = client.Name,
                   },
               });
        public static readonly AuditEventType<string, string, string> UserContentRelatedFileAccess =
            new AuditEventType<string, string, string>(1009, "Content related file access",
                (contentItemId, selectionGroupId, relatedFilePurpose) => new
                {
                    ContentItem = contentItemId,
                    SelectionGroup = selectionGroupId,
                    RelatedFilePurpose = relatedFilePurpose,
                });
        #endregion

        #region Client Admin [2000 - 2999]
        public static readonly AuditEventType<Client, ApplicationUser> UserAssignedToClient = new AuditEventType<Client, ApplicationUser>(
            2001, "User assigned to client", (client, user) => new
            {
                ClientId = client.Id,
                ClientName = client.Name,
                UserId = user.Id,
                UserName = user.UserName,
            });
        public static readonly AuditEventType<Client, ApplicationUser> UserRemovedFromClient = new AuditEventType<Client, ApplicationUser>(
            2002, "User removed from client", (client, user) => new
            {
                ClientId = client.Id,
                UserId = user.Id,
            });
        public static readonly AuditEventType<Client> ClientCreated = new AuditEventType<Client>(
            2003, "Client created", (client) => new
            {
                Client = client,
            });
        public static readonly AuditEventType<Client> ClientEdited = new AuditEventType<Client>(
            2004, "Client edited", (client) => new
            {
                Client = client,
            });
        public static readonly AuditEventType<Client> ClientDeleted = new AuditEventType<Client>(
            2005, "Client deleted", (client) => new
            {
                Client = client,
            });
        public static readonly AuditEventType<Client, ApplicationUser, List<RoleEnum>> ClientRoleAssigned = new AuditEventType<Client, ApplicationUser, List<RoleEnum>>(
            2006, "Client role assigned", (client, user, roles) => new
            {
                ClientId = client.Id,
                UserId = user.Id,
                Role = roles.Select(r => r.ToString()),
            });
        public static readonly AuditEventType<Client, ApplicationUser, List<RoleEnum>> ClientRoleRemoved = new AuditEventType<Client, ApplicationUser, List<RoleEnum>>(
            2007, "Client role removed", (client, user, roles) => new
            {
                ClientId = client.Id,
                UserId = user.Id,
                Role = roles.Select(r => r.ToString()),
            });
        #endregion

        #region User Account [3000 - 3999]
        public static readonly AuditEventType<ApplicationUser> UserAccountCreated = new AuditEventType<ApplicationUser>(
            3001, "User account created", (user) => new
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
            });
        public static readonly AuditEventType<ApplicationUser> UserAccountModified = new AuditEventType<ApplicationUser>(
            3002, "User account modified", (user) => new
            {
                UserId = user.Id,
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<ApplicationUser, string> UserAccountLockByAdmin = new AuditEventType<ApplicationUser, string>(
            3003, "User account lock by Admin", (user, reason) => new
            {
                UserId = user.Id,
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<ApplicationUser> UserAccountDeleted = new AuditEventType<ApplicationUser>(
            3004, "User account deleted", (user) => new
            {
                UserId = user.Id,
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<ApplicationUser> UserAccountEnabled = new AuditEventType<ApplicationUser>(
            3005, "New user account enabled", (user) => new
            {
                UserId = user.Id,
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<ApplicationUser> PasswordResetRequested = new AuditEventType<ApplicationUser>(
            3006, "Account password reset requested", (user) => new
            {
                UserId = user.Id,
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<ApplicationUser> PasswordResetCompleted = new AuditEventType<ApplicationUser>(
            3007, "Account password reset completed", (user) => new
            {
                UserId = user.Id,
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<string> PasswordResetRequestedForInvalidEmail = new AuditEventType<string>(
            3008, "Account password reset requested for invalid email", (email) => new
            {
                RequestedEmail = email,
            });
        public static readonly AuditEventType LoginNotAllowed = new AuditEventType(3009, "Login not allowed");
        public static readonly AuditEventType LoginIsLockedOut = new AuditEventType(3010, "Login account is locked out");

        public static readonly AuditEventType<ApplicationUser> UserPasswordExpired =
            new AuditEventType<ApplicationUser>(3011, "User password expired", (user) =>
                new { userId = user.Id, userName = user.UserName, dateLastSetUtc = user.LastPasswordChangeDateTimeUtc });

        public static readonly AuditEventType<string> LoginIsSuspended = new AuditEventType<string>(
            3012, "Login account is suspended", (attemptedUserName) => new
            {
                AttemptedUsername = attemptedUserName,
            });
        #endregion

        #region Content Access [4000 - 4999]
        public static readonly AuditEventType<SelectionGroup> SelectionGroupCreated = new AuditEventType<SelectionGroup>(
            4001, "Selection group created", (selectionGroup) => new
            {
                SelectionGroup = selectionGroup,
            });
        public static readonly AuditEventType<SelectionGroup> SelectionGroupDeleted = new AuditEventType<SelectionGroup>(
            4002, "Selection group deleted", (selectionGroup) => new
            {
                SelectionGroupId = selectionGroup.Id,
            });
        public static readonly AuditEventType<SelectionGroup, Guid> SelectionGroupUserAssigned = new AuditEventType<SelectionGroup, Guid>(
            4003, "User assigned to selection group", (selectionGroup, userId) => new
            {
                SelectionGroupId = selectionGroup.Id,
                UserId = userId,
            });
        public static readonly AuditEventType<SelectionGroup, Guid> SelectionGroupUserRemoved = new AuditEventType<SelectionGroup, Guid>(
            4004, "User removed from selection group", (selectionGroup, userId) => new
            {
                SelectionGroupId = selectionGroup.Id,
                UserId = userId,
            });
        public static readonly AuditEventType<SelectionGroup, ContentReductionTask> SelectionChangeReductionQueued = new AuditEventType<SelectionGroup, ContentReductionTask>(
            4005, "Selection change reduction task queued", (selectionGroup, reductionTask) => new
            {
                SelectionGroupId = selectionGroup.Id,
                RootContentItemId = selectionGroup.RootContentItemId,
                Action = reductionTask.TaskAction.ToString(),
                SelectedValues = reductionTask.SelectionCriteriaObj.Fields.SelectMany(f => f.Values.Select(v => v.Value)).ToList(),
                SelectedValueIds = reductionTask.SelectionCriteriaObj.Fields.SelectMany(f => f.Values.Select(v => v.Id)).ToList(),
            });
        public static readonly AuditEventType<SelectionGroup, ContentReductionTask> SelectionChangeReductionCanceled = new AuditEventType<SelectionGroup, ContentReductionTask>(
            4006, "Selection change reduction task canceled", (selectionGroup, reductionTask) => new
            {
                SelectionGroupId = selectionGroup.Id,
                RootContentItemId = selectionGroup.RootContentItemId,
                Action = reductionTask.TaskAction.ToString(),
                SelectedValues = reductionTask.SelectionCriteriaObj.Fields.SelectMany(f => f.Values.Select(v => v.Value)).ToList(),
                SelectedValueIds = reductionTask.SelectionCriteriaObj.Fields.SelectMany(f => f.Values.Select(v => v.Id)).ToList(),
            });
        public static readonly AuditEventType<SelectionGroup> SelectionChangeMasterAccessGranted = new AuditEventType<SelectionGroup>(
            4007, "Selection group given master access", (selectionGroup) => new
            {
                SelectionGroupId = selectionGroup.Id,
                RootContentItemId = selectionGroup.RootContentItemId,
            });
        public static readonly AuditEventType<SelectionGroup, bool, string> SelectionGroupSuspensionUpdate = new AuditEventType<SelectionGroup, bool, string>(
            4008, "Selection group suspension status updated", (selectionGroup, isSuspended, reason) => new
            {
                SelectionGroupId = selectionGroup.Id,
                IsSuspended = isSuspended,
                Reason = reason,
            });
        public static readonly AuditEventType<ContentReductionTask> SelectionChangeReductionLive = new AuditEventType<ContentReductionTask>(
            4009, "Selection change reduction task go live succeeded", (task) => new
            {
                ContentReductionTaskId = task.Id,
            });
        public static readonly AuditEventType<List<UserInSelectionGroup>, Guid>
        ContentDisclaimerAcceptanceResetTextChange =
            new AuditEventType<List<UserInSelectionGroup>, Guid>(
                4101, "Content disclaimer acceptance reset", (usersInGroup, rootContentItemId) => new
                {
                    RootContentItemId = rootContentItemId,
                    UsersInGroup = usersInGroup.Select(u => new
                    {
                        UserInSelectionGroupId = u.Id,
                        UserId = u.UserId,
                        SelectionGroupId = u.SelectionGroupId,
                    }),
                    Reason = "Content disclaimer text was changed",
                });
        public static readonly AuditEventType<List<UserInSelectionGroup>, Guid>
        ContentDisclaimerAcceptanceResetRepublish =
            new AuditEventType<List<UserInSelectionGroup>, Guid>(
                4101, "Content disclaimer acceptance reset", (usersInGroup, rootContentItemId) => new
                {
                    RootContentItemId = rootContentItemId,
                    UsersInGroup = usersInGroup.Select(u => new
                    {
                        UserInSelectionGroupId = u.Id,
                        UserId = u.UserId,
                        SelectionGroupId = u.SelectionGroupId,
                    }),
                    Reason = "Root content item was republished",
                });
        public static readonly AuditEventType<List<UserInSelectionGroup>, Guid>
        ContentDisclaimerAcceptanceResetSelectionChange =
            new AuditEventType<List<UserInSelectionGroup>, Guid>(
                4101, "Content disclaimer acceptance reset", (usersInGroup, rootContentItemId) => new
                {
                    RootContentItemId = rootContentItemId,
                    UsersInGroup = usersInGroup.Select(u => new
                    {
                        UserInSelectionGroupId = u.Id,
                        UserId = u.UserId,
                        SelectionGroupId = u.SelectionGroupId,
                    }),
                    Reason = "Selections were changed",
                });
        public static readonly AuditEventType<List<UserInSelectionGroup>, Guid>
            ContentDisclaimerAcceptanceResetRemovedFromGroup =
            new AuditEventType<List<UserInSelectionGroup>, Guid>(
                4101, "Content disclaimer acceptance reset", (usersInGroup, rootContentItemId) => new
                {
                    RootContentItemId = rootContentItemId,
                    UsersInGroup = usersInGroup.Select(u => new
                    {
                        UserInSelectionGroupId = u.Id,
                        UserId = u.UserId,
                        SelectionGroupId = u.SelectionGroupId,
                    }),
                    Reason = "User removed from selection group",
                });
        public static readonly AuditEventType<UserInSelectionGroup, string, string> ContentDisclaimerPresented =
            new AuditEventType<UserInSelectionGroup, string, string>(4102, "Content disclaimer presented to user",
                (userInSelectionGroup, validationId, disclaimerText) => new
                {
                    ValidationId = validationId,
                    UserInSelectionGroup = new
                    {
                        Id = userInSelectionGroup.Id,
                        UserId = userInSelectionGroup.UserId,
                        SelectionGroupId = userInSelectionGroup.SelectionGroupId,
                    },
                    DisclaimerText = disclaimerText,
                });
        public static readonly AuditEventType<UserInSelectionGroup, string> ContentDisclaimerAccepted =
            new AuditEventType<UserInSelectionGroup, string>(4103, "Content disclaimer accepted by user",
                (userInSelectionGroup, validationId) => new
                {
                    ValidationId = validationId,
                    UserInSelectionGroup = new
                    {
                        Id = userInSelectionGroup.Id,
                        UserId = userInSelectionGroup.UserId,
                        SelectionGroupId = userInSelectionGroup.SelectionGroupId,
                    },
                });

        #endregion

        #region Publishing Server [5000 - 5999]
        // 50xx - Preliminary events
        public static readonly AuditEventType<object> ReductionValidationFailed = new AuditEventType<object>(
            5001, "Reduction validation Failed", (logObject) => logObject);

        // 51xx - Hierarchy extraction and content reduction events
        public static readonly AuditEventType<object> HierarchyExtractionSucceeded = new AuditEventType<object>(
            5101, "Content hierarchy extraction completed", (logObject) => logObject);
        public static readonly AuditEventType<object> HierarchyExtractionFailed = new AuditEventType<object>(
            5102, "Content hierarchy extraction failed", (logObject) => logObject);
        public static readonly AuditEventType<object> ContentFileReductionSucceeded = new AuditEventType<object>(
            5103, "Content file reduction completed", (logObject) => logObject);
        public static readonly AuditEventType<object> ContentFileReductionFailed = new AuditEventType<object>(
            5104, "Content file reduction failed", (logObject) => logObject);

        // 52xx - Reduction task aggregate outcome events
        public static readonly AuditEventType<object> ContentReductionTaskCanceled = new AuditEventType<object>(
            5201, "Content reduction task canceled", (logObject) => logObject);
        public static readonly AuditEventType<object> PublicationRequestProcessingSuccess = new AuditEventType<object>(
            5202, "Content publication request success", (logObject) => logObject);

        // 53xx - Publication request aggregate outcome events
        public static readonly AuditEventType<object> ContentPublicationRequestCanceled = new AuditEventType<object>(
            5301, "Content publication request canceled", (logObject) => logObject);
        #endregion

        #region Content Publishing [6000 - 6999]
        public static readonly AuditEventType<RootContentItem> RootContentItemCreated = new AuditEventType<RootContentItem>(
            6001, "Root content item created", (rootContentItem) => new
            {
                RootContentItemId = rootContentItem.Id,
                ContentTypeId = rootContentItem.ContentTypeId,
                ClientId = rootContentItem.ClientId,
            });
        public static readonly AuditEventType<RootContentItem> RootContentItemDeleted = new AuditEventType<RootContentItem>(
            6002, "Root content item deleted", (rootContentItem) => new
            {
                RootContentItemId = rootContentItem.Id,
                ContentTypeId = rootContentItem.ContentTypeId,
                ClientId = rootContentItem.ClientId,
            });
        public static readonly AuditEventType<RootContentItem> RootContentItemUpdated = new AuditEventType<RootContentItem>(
            6003, "Root content item updated", (rootContentItem) => new
            {
                RootContentItemId = rootContentItem.Id,
                RootContentName = rootContentItem.ContentName,
                RootContentDescription = rootContentItem.Description,
                RootContentNotes = rootContentItem.Notes,
                RootContentDisclaimer = rootContentItem.ContentDisclaimer,
            });
        public static readonly AuditEventType<RootContentItem, ContentPublicationRequest> PublicationRequestInitiated = new AuditEventType<RootContentItem, ContentPublicationRequest>(
            6101, "Publication request initiated", (rootContentItem, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                ContentItemId = rootContentItem.Id,
                ContentItemName = rootContentItem.ContentName,
                Uploads = publicationRequest.UploadedRelatedFilesObj,
            });
        public static readonly AuditEventType<RootContentItem, ContentPublicationRequest> PublicationCanceled = new AuditEventType<RootContentItem, ContentPublicationRequest>(
            6102, "Publication request canceled", (rootContentItem, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                ContentItemId = rootContentItem.Id,
                ContentItemName = rootContentItem.ContentName,
            });
        public static readonly AuditEventType<RootContentItem, ContentPublicationRequest> GoLiveValidationFailed = new AuditEventType<RootContentItem, ContentPublicationRequest>(
            6103, "GoLive Validation Failed", (rootContentItem, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                RootContentItemId = rootContentItem.Id,
            });
        public static readonly AuditEventType<SelectionGroup, ContentRelatedFile,string> ChecksumInvalid = new AuditEventType<SelectionGroup, ContentRelatedFile,string>(
            6104, "Checksum Invalid", (selectionGroup, contentRelatedFile, sourceAction) => new
            {
                SelectionGroupId = selectionGroup.Id,
                RootContentItemId = selectionGroup.RootContentItemId,
                FilePath = contentRelatedFile.FullPath,
                FilePurpose = contentRelatedFile.FilePurpose,
                Action = sourceAction,
            });
        public static readonly AuditEventType<RootContentItem, ContentPublicationRequest, string> ContentPublicationGoLive = new AuditEventType<RootContentItem, ContentPublicationRequest, string>(
            6105, "Content publication golive", (rootContentItem, publicationRequest, summaryGUID) => new
            {
                SummaryGUID = summaryGUID,
            });
        public static readonly AuditEventType<PreLiveContentValidationSummaryLogModel> PreGoLiveSummary = new AuditEventType<PreLiveContentValidationSummaryLogModel>(
            6106, "Content publication pre-golive summary", preliveSummary => preliveSummary);
        public static readonly AuditEventType<RootContentItem, ContentPublicationRequest> ContentPublicationRejected = new AuditEventType<RootContentItem, ContentPublicationRequest>(
            6107, "Content publication rejected", (rootContentItem, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                RootContentItemId = rootContentItem.Id,
            });
        #endregion

        #region System Admin [7000 - 7999]
        public static readonly AuditEventType<ApplicationUser, bool, string> UserSuspensionUpdate = new AuditEventType<ApplicationUser, bool, string>(
            7001, "User suspension status updated", (user, isSuspended, reason) => new
            {
                UserId = user.Id,
                IsSuspended = isSuspended,
                Reason = reason,
            });
        public static readonly AuditEventType<RootContentItem, bool, string> RootContentItemSuspensionUpdate = new AuditEventType<RootContentItem, bool, string>(
            7002, "Root content item suspension status updated", (item, isSuspended, reason) => new
            {
                RootContentItemId = item.Id,
                IsSuspended = isSuspended,
                Reason = reason,
            });
        public static readonly AuditEventType<ApplicationUser, RoleEnum> SystemRoleAssigned = new AuditEventType<ApplicationUser, RoleEnum>(
            7003, "System role assigned", (user, role) => new
            {
                UserId = user.Id,
                Role = role.ToString(),
            });
        public static readonly AuditEventType<ApplicationUser, RoleEnum> SystemRoleRemoved = new AuditEventType<ApplicationUser, RoleEnum>(
            7004, "System role removed", (user, role) => new
            {
                UserId = user.Id,
                Role = role.ToString(),
            });
        public static readonly AuditEventType<ProfitCenter> ProfitCenterCreated = new AuditEventType<ProfitCenter>(
            7101, "Profit center created", (profitCenter) => new
            {
                ProfitCenter = profitCenter,
            });
        public static readonly AuditEventType<ProfitCenter> ProfitCenterUpdated = new AuditEventType<ProfitCenter>(
            7102, "Profit center updated", (profitCenter) => new
            {
                ProfitCenter = profitCenter,
            });
        public static readonly AuditEventType<ProfitCenter> ProfitCenterDeleted = new AuditEventType<ProfitCenter>(
            7103, "Profit center deleted", (profitCenter) => new
            {
                ProfitCenterId = profitCenter.Id,
            });
        public static readonly AuditEventType<ProfitCenter, ApplicationUser> UserAssignedToProfitCenter = new AuditEventType<ProfitCenter, ApplicationUser>(
            7104, "User assigned to profit center", (profitCenter, user) => new
            {
                ProfitCenterId = profitCenter.Id,
                ProfitCenterName = profitCenter.Name,
                UserId = user.Id,
                UserName = user.UserName,
            });
        public static readonly AuditEventType<ProfitCenter, ApplicationUser> UserRemovedFromProfitCenter = new AuditEventType<ProfitCenter, ApplicationUser>(
            7105, "User removed from profit center", (profitCenter, user) => new
            {
                ProfitCenterId = profitCenter.Id,
                UserId = user.Id,
            });

        // 72xx - Authentication scheme management
        public static readonly AuditEventType<AuthenticationScheme> NewAuthenticationSchemeAdded = new AuditEventType<AuthenticationScheme>(
            7201, "New authentication scheme added", scheme => new
            {
                SchemeId = scheme.Id,
                SchemeName = scheme.Name,
                scheme.DomainList,
                scheme.DisplayName,
                Type = scheme.Type.ToString(),
                SchemeProperties = scheme.SchemePropertiesObj,
            });
        public static readonly AuditEventType<AuthenticationScheme,AuthenticationScheme> AuthenticationSchemeUpdated = new AuditEventType<AuthenticationScheme,AuthenticationScheme>(
            7202, "Authentication scheme updated", (before,after) => new
            {
                before = new
                {
                    SchemeId = before.Id,
                    SchemeName = before.Name,
                    before.DomainList,
                    before.DisplayName,
                    Type = before.Type.ToString(),
                    SchemeProperties = before.SchemePropertiesObj,
                },
                after = new
                {
                    SchemeId = after.Id,
                    SchemeName = after.Name,
                    after.DomainList,
                    after.DisplayName,
                    Type = after.Type.ToString(),
                    SchemeProperties = after.SchemePropertiesObj,
                }
            });

        // 73xx - Client management
        public static readonly AuditEventType<UpdateClientDomainLimitLogModel> ClientDomainLimitUpdated = new AuditEventType<UpdateClientDomainLimitLogModel>(
            7301, "Client domain limit updated", logModel => logModel);
        #endregion
        #endregion

        #region Common loggable object declarations
        public class SelectionGroupLogObject
        {
            public Guid ClientId { get; set; }
            public Guid RootContentItemId { get; set; }
            public Guid SelectionGroupId { get; set; }
        }
        #endregion

        private readonly Func<object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<object> logObjectTransform = null) : base(id, name) {
            this.logObjectTransform = logObjectTransform ?? new Func<object>(() => new { });
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public new AuditEvent ToEvent(
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = base.ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform();

            return auditEvent;
        }
    }

    // Generics represent what entities are required to log this event
    public sealed class AuditEventType<P1> : AuditEventTypeBase
    {
        private readonly Func<P1, object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<P1, object> logObjectTransform) : base(id, name) {
            this.logObjectTransform = logObjectTransform;
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public AuditEvent ToEvent(P1 param1,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform(param1);

            return auditEvent;
        }
    }

    public sealed class AuditEventType<P1, P2> : AuditEventTypeBase
    {
        private readonly Func<P1, P2, object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<P1, P2, object> logObjectTransform) : base(id, name) {
            this.logObjectTransform = logObjectTransform;
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public AuditEvent ToEvent(P1 param1, P2 param2,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform(param1, param2);

            return auditEvent;
        }
    }

    public sealed class AuditEventType<P1, P2, P3> : AuditEventTypeBase
    {
        private readonly Func<P1, P2, P3, object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<P1, P2, P3, object> logObjectTransform) : base(id, name) {
            this.logObjectTransform = logObjectTransform;
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public AuditEvent ToEvent(P1 param1, P2 param2, P3 param3,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform(param1, param2, param3);

            return auditEvent;
        }
    }

    public sealed class AuditEventType<P1, P2, P3, P4> : AuditEventTypeBase
    {
        private readonly Func<P1, P2, P3, P4, object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<P1, P2, P3, P4, object> logObjectTransform) : base(id, name)
        {
            this.logObjectTransform = logObjectTransform;
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public AuditEvent ToEvent(P1 param1, P2 param2, P3 param3, P4 param4,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform(param1, param2, param3, param4);

            return auditEvent;
        }
    }

    public sealed class AuditEventType<P1, P2, P3, P4, P5> : AuditEventTypeBase
    {
        private readonly Func<P1, P2, P3, P4, P5, object> logObjectTransform;

        /// <summary>
        /// Represents a class of loggable event.
        /// </summary>
        /// <param name="id">AuditEvent ID. This value is logged and is used to uniquely identify this event type.</param>
        /// <param name="name">Name of the event type.</param>
        /// <param name="logObjectTransform">Defines the log object for this event type.</param>
        public AuditEventType(int id, string name, Func<P1, P2, P3, P4, P5, object> logObjectTransform) : base(id, name)
        {
            this.logObjectTransform = logObjectTransform;
        }

        /// <summary>
        /// Create an audit event based on this event type.
        /// </summary>
        /// <param name="callerName">Calling method or property. Determined by the compiler if not supplied.</param>
        /// <param name="callerPath">Absolute path of the calling file. Determined by the compiler if not supplied.</param>
        /// <param name="callerLine">Line of the calling file. Determined by the compiler if not supplied.</param>
        /// <returns>AuditEvent</returns>
        public AuditEvent ToEvent(P1 param1, P2 param2, P3 param3, P4 param4, P5 param5,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            var auditEvent = ToEvent(callerName, callerPath, callerLine);
            auditEvent.EventDataObject = logObjectTransform(param1, param2, param3, param4, param5);

            return auditEvent;
        }
    }
}

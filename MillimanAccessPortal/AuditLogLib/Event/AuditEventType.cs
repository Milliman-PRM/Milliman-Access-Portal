/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES:
 *   Guidelines for useful log events:
 *  
 *      1) If a content item, selection group, user, or client is 
 *          relevant to the event, include it in the log
 *          
 *      2) If you include one or more of those items, include both
 *          the Id and Name (or equivalent) properties - UserName for users
 */

using AuditLogLib.Models;
using MapDbContextLib.Models;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AuditLogLib.Event
{
    #region Common type declarations used in audit logging
    public class SelectionGroupLogObject
    {
        public Guid ClientId { get; set; }
        public Guid RootContentItemId { get; set; }
        public Guid SelectionGroupId { get; set; }
    }

    public enum PasswordResetRequestReason
    {
        [Display(Name = "User Initiated")]
        UserInitiated,

        [Display(Name = "Password Expired")]
        PasswordExpired,

        [Display(Name = "Previous Password Reset Token Invalid")]
        PasswordResetTokenInvalid,
    }

    public enum ContentDisclaimerResetReason
    {
        [Display(Name = "Content disclaimer text was changed")]
        DisclaimerTextModified,

        [Display(Name = "Root content item was republished")]
        ContentItemRepublished,

        [Display(Name = "Selections were changed")]
        ContentSelectionsModified,

        [Display(Name = "User removed from selection group")]
        UserRemovedFromSelectionGroup,
    }
    #endregion

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
                       selectionGroup.Id,
                       selectionGroup.GroupName,
                   },
                   ContentItem = new
                   {
                       contentItem.Id,
                       contentItem.ContentName,
                   },
                   Client = new
                   {
                       client.Id,
                       client.Name,
                   },
               });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, string> UserContentRelatedFileAccess =
            new AuditEventType<SelectionGroup, RootContentItem, Client, string>(1009, "Content related file access",
                (selectionGroup, contentItem, client, relatedFilePurpose) => new
                {
                    SelectionGroup = new
                    {
                        selectionGroup.Id,
                        selectionGroup.GroupName,
                    },
                    ContentItem = new
                    {
                        contentItem.Id,
                        contentItem.ContentName,
                    },
                    Client = new
                    {
                        client.Id,
                        client.Name,
                    },
                    RelatedFilePurpose = relatedFilePurpose,
                });
        #endregion

        #region Client Admin [2000 - 2999]
        public static readonly AuditEventType<Client, ApplicationUser> UserAssignedToClient = new AuditEventType<Client, ApplicationUser>(
            2001, "User assigned to client", (client, user) => new
            {
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                User = new
                {
                    user.Id,
                    user.UserName,
                }
            });
        public static readonly AuditEventType<Client, ApplicationUser> UserRemovedFromClient = new AuditEventType<Client, ApplicationUser>(
            2002, "User removed from client", (client, user) => new
            {
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                User = new
                {
                    user.Id,
                    user.UserName,
                }
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
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                Role = roles.Select(r => r.ToString()),
            });
        public static readonly AuditEventType<Client, ApplicationUser, List<RoleEnum>> ClientRoleRemoved = new AuditEventType<Client, ApplicationUser, List<RoleEnum>>(
            2007, "Client role removed", (client, user, roles) => new
            {
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                Role = roles.Select(r => r.ToString()),
            });
        #endregion

        #region User Account [3000 - 3999]
        public static readonly AuditEventType<ApplicationUser> UserAccountCreated = new AuditEventType<ApplicationUser>(
            3001, "User account created", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                }
            });
        public static readonly AuditEventType<ApplicationUser> UserAccountModified = new AuditEventType<ApplicationUser>(
            3002, "User account modified", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                }
            });
        public static readonly AuditEventType<ApplicationUser, string> UserAccountLockByAdmin = new AuditEventType<ApplicationUser, string>(
            3003, "User account lock by Admin", (user, reason) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                reason,
            });
        public static readonly AuditEventType<ApplicationUser> UserAccountDeleted = new AuditEventType<ApplicationUser>(
            3004, "User account deleted", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
            });
        public static readonly AuditEventType<ApplicationUser> UserAccountEnabled = new AuditEventType<ApplicationUser>(
            3005, "New user account enabled", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
            });
        public static readonly AuditEventType<ApplicationUser,PasswordResetRequestReason> PasswordResetRequested = new AuditEventType<ApplicationUser, PasswordResetRequestReason>(
            3006, "Account password reset requested", (user,reason) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                Reason = reason.GetDisplayNameString()
            });
        public static readonly AuditEventType<ApplicationUser> PasswordResetCompleted = new AuditEventType<ApplicationUser>(
            3007, "Account password reset completed", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
            });
        public static readonly AuditEventType<string> PasswordResetRequestedForInvalidEmail = new AuditEventType<string>(
            3008, "Account password reset requested for invalid email", (email) => new
            {
                RequestedEmail = email,
            });
        public static readonly AuditEventType LoginNotAllowed = new AuditEventType(3009, "Login not allowed");
        public static readonly AuditEventType LoginIsLockedOut = new AuditEventType(3010, "Login account is locked out");

        public static readonly AuditEventType<ApplicationUser> UserPasswordExpired =
            new AuditEventType<ApplicationUser>(3011, "User password expired", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                dateLastSetUtc = user.LastPasswordChangeDateTimeUtc
            });

        public static readonly AuditEventType<string> LoginIsSuspended = new AuditEventType<string>(
            3012, "Login account is suspended", (attemptedUserName) => new
            {
                attemptedUserName,
            });

        public static readonly AuditEventType<UserAgreementLogModel> UserAgreementPresented =
            new AuditEventType<UserAgreementLogModel>(3101, "User agreement presented to user",
                (UserAgreementLogModel) => new
                {
                    UserAgreementLogModel.ValidationId,
                    UserAgreementLogModel.AgreementText,
                });
        public static readonly AuditEventType<Guid> UserAgreementAcceptance = new AuditEventType<Guid>(
            3102, "User agreement acceptance", (validationId) => new
            {
                ValidationId = validationId.ToString(),
            });
        public static readonly AuditEventType<Guid> UserAgreementDeclined = new AuditEventType<Guid>(
            3103, "User agreement declined", (validationId) => new
            {
                ValidationId = validationId.ToString(),
            });
        public static readonly AuditEventType<ApplicationUser> UserAgreementReset = new AuditEventType<ApplicationUser>(
            3104, "User agreement reset", (user) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
            });
        #endregion

        #region Content Access [4000 - 4999]
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client> SelectionGroupCreated = new AuditEventType<SelectionGroup, RootContentItem, Client>(
            4001, "Selection group created", (selectionGroup, contentItem, client) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                    selectionGroup.IsInactive,
                    selectionGroup.IsMaster,
                    selectionGroup.IsSuspended,
                    selectionGroup.ReducedContentChecksum,
                    selectionGroup.SelectedHierarchyFieldValueList,
                    selectionGroup.ContentInstanceUrl,
                },
                RootContentItem = new
                {
                    selectionGroup.RootContentItem.Id,
                    selectionGroup.RootContentItem.ContentName,
                },
                Client = new
                {
                    selectionGroup.RootContentItem.Client.Id,
                    selectionGroup.RootContentItem.Client.Name,
                }
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client> SelectionGroupDeleted = new AuditEventType<SelectionGroup, RootContentItem, Client>(
            4002, "Selection group deleted", (selectionGroup, rootContentItem, client) => new
            {
                SelectionGroup = new 
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new 
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, ApplicationUser> SelectionGroupUserAssigned = new AuditEventType<SelectionGroup, RootContentItem, Client, ApplicationUser>(
            4003, "User assigned to selection group", (selectionGroup, rootContentItem, client, user) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, ApplicationUser> SelectionGroupUserRemoved = new AuditEventType<SelectionGroup, RootContentItem, Client, ApplicationUser>(
            4004, "User removed from selection group", (selectionGroup, rootContentItem, client, user) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, ContentReductionTask> SelectionChangeReductionQueued = new AuditEventType<SelectionGroup, RootContentItem, Client, ContentReductionTask>(
            4005, "Selection change reduction task queued", (selectionGroup, rootContentItem, client, reductionTask) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                Action = reductionTask.TaskAction.ToString(),
                SelectedValues = reductionTask.SelectionCriteriaObj
                                              .Fields
                                              .SelectMany(f => f.Values.Where(v => v.SelectionStatus).Select(v => new { v.Id, v.Value }))
                                              .ToList(),
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, ContentReductionTask> SelectionChangeReductionCanceled = new AuditEventType<SelectionGroup, RootContentItem, Client, ContentReductionTask>(
            4006, "Selection change reduction task canceled", (selectionGroup, rootContentItem, client, reductionTask) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                Action = reductionTask.TaskAction.ToString(),
                SelectedValues = reductionTask.SelectionCriteriaObj
                                              ?.Fields
                                              .SelectMany(f => f.Values.Where(v => v.SelectionStatus).Select(v => new { v.Id, v.Value }))
                                              .ToList(),
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client> SelectionChangeMasterAccessGranted = new AuditEventType<SelectionGroup, RootContentItem, Client>(
            4007, "Selection group given master access", (selectionGroup, rootContentItem, client) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, bool> SelectionGroupSuspensionUpdate = new AuditEventType<SelectionGroup, RootContentItem, Client, bool>(
            4008, "Selection group suspension status updated", (selectionGroup, rootContentItem, client, isSuspended) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                IsSuspended = isSuspended,
            });
        public static readonly AuditEventType<ContentReductionTask> SelectionChangeReductionLive = new AuditEventType<ContentReductionTask>(
            4009, "Selection change reduction task go live succeeded", (task) => new
            {
                ContentReductionTaskId = task.Id,
            });
        public static readonly AuditEventType<List<UserInSelectionGroup>, RootContentItem, Client, ContentDisclaimerResetReason>
        ContentDisclaimerAcceptanceReset = new AuditEventType<List<UserInSelectionGroup>, RootContentItem, Client, ContentDisclaimerResetReason>(
            4101, "Content disclaimer acceptance reset", (usersInGroup, rootContentItem, client, reason) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                UserInSelectionGroup = usersInGroup.Select(u => new
                {
                    u.Id,
                    User = new
                    {
                        u.UserId,
                        u.User.UserName,
                    },
                    SelectionGroup = new
                    {
                        u.SelectionGroup.Id,
                        u.SelectionGroup.GroupName,
                    }
                }),
                Reason = reason.GetDisplayNameString(),
            });
        public static readonly AuditEventType<UserInSelectionGroup, RootContentItem, Client, Guid, string> ContentDisclaimerPresented =
            new AuditEventType<UserInSelectionGroup, RootContentItem, Client, Guid, string>(4102, "Content disclaimer presented to user",
                (userInSelectionGroup, rootContentItem, client, validationId, disclaimerText) => new
                {
                    ValidationId = validationId.ToString(),
                    UserInSelectionGroup = new
                    {
                        userInSelectionGroup.Id,
                        User = new
                        {
                            userInSelectionGroup.User.Id,
                            userInSelectionGroup.User.UserName,
                        },
                        SelectionGroup = new
                        {
                            userInSelectionGroup.SelectionGroup.Id,
                            userInSelectionGroup.SelectionGroup.GroupName,
                        }
                    },
                    RootContentItem = new
                    {
                        rootContentItem.Id,
                        rootContentItem.ContentName,
                    },
                    Client = new
                    {
                        client.Id,
                        client.Name,
                    },
                    DisclaimerText = disclaimerText,
                });
        public static readonly AuditEventType<UserInSelectionGroup, RootContentItem, Client, Guid> ContentDisclaimerAccepted =
            new AuditEventType<UserInSelectionGroup, RootContentItem, Client, Guid>(4103, "Content disclaimer accepted by user",
                (userInSelectionGroup, rootContentItem, client, validationId) => new
                {
                    ValidationId = validationId,
                    UserInSelectionGroup = new
                    {
                        userInSelectionGroup.Id,
                        User = new
                        {
                            userInSelectionGroup.User.Id,
                            userInSelectionGroup.User.UserName,
                        },
                        SelectionGroup = new
                        {
                            userInSelectionGroup.SelectionGroup.Id,
                            userInSelectionGroup.SelectionGroup.GroupName,
                        }
                    },
                    RootContentItem = new
                    {
                        rootContentItem.Id,
                        rootContentItem.ContentName,
                    },
                    Client = new
                    {
                        client.Id,
                        client.Name,
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
        public static readonly AuditEventType<RootContentItem, Client> RootContentItemCreated = new AuditEventType<RootContentItem, Client>(
            6001, "Root content item created", (rootContentItem, client) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                    rootContentItem.Description,
                    rootContentItem.Notes,
                    rootContentItem.ContentDisclaimer,
                },
                ContentType = new
                {
                    rootContentItem.ContentType.Id,
                    rootContentItem.ContentType.Name,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<RootContentItem, Client> RootContentItemDeleted = new AuditEventType<RootContentItem, Client>(
            6002, "Root content item deleted", (rootContentItem, client) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                ContentType = new
                {
                    rootContentItem.ContentType.Id,
                    rootContentItem.ContentType.Name,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<RootContentItem, Client> RootContentItemUpdated = new AuditEventType<RootContentItem, Client>(
            6003, "Root content item updated", (rootContentItem, client) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                    rootContentItem.Description,
                    rootContentItem.Notes,
                    rootContentItem.ContentDisclaimer,
                },
                ContentType = new
                {
                    rootContentItem.ContentType.Id,
                    rootContentItem.ContentType.Name,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<RootContentItem, Client, ContentPublicationRequest> PublicationRequestInitiated = new AuditEventType<RootContentItem, Client, ContentPublicationRequest>(
            6101, "Publication request initiated", (rootContentItem, client, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                Uploads = publicationRequest.UploadedRelatedFilesObj,
            });
        public static readonly AuditEventType<RootContentItem, Client, ContentPublicationRequest> PublicationCanceled = new AuditEventType<RootContentItem, Client, ContentPublicationRequest>(
            6102, "Publication request canceled", (rootContentItem, client, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<RootContentItem, Client, ContentPublicationRequest> GoLiveValidationFailed = new AuditEventType<RootContentItem, Client, ContentPublicationRequest>(
            6103, "GoLive Validation Failed", (rootContentItem, client, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        public static readonly AuditEventType<SelectionGroup, RootContentItem, Client, ContentRelatedFile, string> ChecksumInvalid = new AuditEventType<SelectionGroup, RootContentItem, Client, ContentRelatedFile, string>(
            6104, "Checksum Invalid", (selectionGroup, rootContentItem, client, contentRelatedFile, SourceAction) => new
            {
                SelectionGroup = new
                {
                    selectionGroup.Id,
                    selectionGroup.GroupName,
                },
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                contentRelatedFile.FullPath,
                contentRelatedFile.FilePurpose,
                SourceAction,
            });
        public static readonly AuditEventType<RootContentItem, Client, ContentPublicationRequest, string> ContentPublicationGoLive = new AuditEventType<RootContentItem, Client, ContentPublicationRequest, string>(
            6105, "Content publication golive", (rootContentItem, client, publicationRequest, summaryGUID) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                SummaryGUID = summaryGUID,
            });
        public static readonly AuditEventType<PreLiveContentValidationSummaryLogModel> PreGoLiveSummary = new AuditEventType<PreLiveContentValidationSummaryLogModel>(
            6106, "Content publication pre-golive summary", preliveSummary => preliveSummary);
        public static readonly AuditEventType<RootContentItem, Client, ContentPublicationRequest> ContentPublicationRejected = new AuditEventType<RootContentItem, Client, ContentPublicationRequest>(
            6107, "Content publication rejected", (rootContentItem, client, publicationRequest) => new
            {
                PublicationRequestId = publicationRequest.Id,
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
            });
        #endregion

        #region System Admin [7000 - 7999]
        public static readonly AuditEventType<ApplicationUser, bool, string> UserSuspensionUpdate = new AuditEventType<ApplicationUser, bool, string>(
            7001, "User suspension status updated", (user, isSuspended, reason) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                IsSuspended = isSuspended,
                Reason = reason,
            });
        public static readonly AuditEventType<RootContentItem, Client, bool, string> RootContentItemSuspensionUpdate = new AuditEventType<RootContentItem, Client, bool, string>(
            7002, "Root content item suspension status updated", (rootContentItem, client, isSuspended, reason) => new
            {
                RootContentItem = new
                {
                    rootContentItem.Id,
                    rootContentItem.ContentName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                IsSuspended = isSuspended,
                Reason = reason,
            });
        public static readonly AuditEventType<ApplicationUser, RoleEnum> SystemRoleAssigned = new AuditEventType<ApplicationUser, RoleEnum>(
            7003, "System role assigned", (user, role) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
                Role = role.ToString(),
            });
        public static readonly AuditEventType<ApplicationUser, RoleEnum> SystemRoleRemoved = new AuditEventType<ApplicationUser, RoleEnum>(
            7004, "System role removed", (user, role) => new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                },
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
                ProfitCenter = new
                {
                    profitCenter.Id,
                    profitCenter.Name,
                }
            });
        public static readonly AuditEventType<ProfitCenter, ApplicationUser> UserAssignedToProfitCenter = new AuditEventType<ProfitCenter, ApplicationUser>(
            7104, "User assigned to profit center", (profitCenter, user) => new
            {
                ProfitCenter = new
                {
                    profitCenter.Id,
                    profitCenter.Name,
                },
                User = new
                {
                    user.Id,
                    user.UserName,
                },
            });
        public static readonly AuditEventType<ProfitCenter, ApplicationUser> UserRemovedFromProfitCenter = new AuditEventType<ProfitCenter, ApplicationUser>(
            7105, "User removed from profit center", (profitCenter, user) => new
            {
                ProfitCenter = new
                {
                    profitCenter.Id,
                    profitCenter.Name,
                },
                User = new
                {
                    user.Id,
                    user.UserName,
                },
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
                Before = new
                {
                    SchemeId = before.Id,
                    SchemeName = before.Name,
                    before.DomainList,
                    before.DisplayName,
                    Type = before.Type.ToString(),
                    SchemeProperties = before.SchemePropertiesObj,
                },
                After = new
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

        // 74xx - Global system management
        public static readonly AuditEventType<string> UserAgreementUpdated = new AuditEventType<string>(
            7401, "User agreement updated", NewText => NewText);
        #endregion

        #region File Drop [8000 - 8999]
        // 80xx - FileDrop admin events
        public static readonly AuditEventType<FileDrop, Guid, string> FileDropCreated = new AuditEventType<FileDrop, Guid, string>(
            8001, "File Drop Created", (fileDrop, clientId, clientName) => new
            {
                FileDrop = (FileDropLogModel)fileDrop,
                Client = new
                {
                    clientId,
                    clientName,
                },
            });

        /// <summary>
        /// sftpAccounts expected to have navigation property ApplicationUser populated
        /// </summary>
        public static readonly AuditEventType<FileDrop, Client, IEnumerable<SftpAccount>> FileDropDeleted = new AuditEventType<FileDrop, Client, IEnumerable<SftpAccount>>(
            8002, "File Drop Deleted", (fileDrop, client, sftpAccounts) => new
            {
                FileDrop = (FileDropLogModel)fileDrop,
                Client = new { client.Id, client.Name, },
                AffectedSftpAccounts = sftpAccounts.Select(a => new
                    { 
                        SftpAccount = new { a.Id, a.UserName },
                        MapUser = a.ApplicationUserId.HasValue ? new { Id = a.ApplicationUserId.Value, a?.ApplicationUser?.UserName } 
                                                               : null
                    }),
            });

        public static readonly AuditEventType<FileDrop, FileDrop, Guid, string> FileDropUpdated = new AuditEventType<FileDrop, FileDrop, Guid, string>(
            8003, "File Drop Updated", (oldFileDrop, newFileDrop, clientId, clientName) => new
            {
                OldFileDrop = (FileDropLogModel)oldFileDrop,
                NewFileDrop = (FileDropLogModel)newFileDrop,
                Client = new
                {
                    clientId,
                    clientName,
                },
            });

        /// <summary>
        /// sftpAccounts expected to have navigation property ApplicationUser populated
        /// </summary>
        public static readonly AuditEventType<SftpAccount, FileDropUserPermissionGroup, FileDrop> SftpAccountAuthenticated = new AuditEventType<SftpAccount, FileDropUserPermissionGroup, FileDrop>(
            8010, "SFTP Account Authenticated", (account, permissionGroup, fileDrop) => new
            {
                PermissionGroup = new
                {
                    permissionGroup.Id,
                    permissionGroup.IsPersonalGroup,
                    permissionGroup.ReadAccess,
                    permissionGroup.WriteAccess,
                    permissionGroup.DeleteAccess,
                },
                FileDrop = new 
                {
                    fileDrop.Id,
                    fileDrop.Name,
                    fileDrop.RootPath,
                },
                SftpAccount = new
                {
                    account.Id,
                    account.UserName,
                },
                MapUser = account.ApplicationUserId.HasValue ? new { Id = account.ApplicationUserId.Value, account.ApplicationUser?.UserName }
                                                             : null,
            });

        public static readonly AuditEventType<FileDropDirectory, FileDropLogModel, SftpAccount, Client, ApplicationUser?> SftpDirectoryCreated = new AuditEventType<FileDropDirectory, FileDropLogModel, SftpAccount, Client, ApplicationUser?>(
            8014, "SFTP Directory Created", (fileDropDirectory, fileDropModel, sftpAccount, client, mapUser) => new
            {
                FileDropDirectory = (FileDropDirectoryLogModel)fileDropDirectory,
                FileDrop = fileDropModel,
                SftpAccount = new 
                {
                    sftpAccount.Id,
                    sftpAccount.UserName,
                },
                Client = new
                {
                    client.Id,
                    client.Name,
                },
                MapUser = mapUser != null 
                    ? new { mapUser.Id, mapUser.UserName, }
                    : null,
            });

        #endregion
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

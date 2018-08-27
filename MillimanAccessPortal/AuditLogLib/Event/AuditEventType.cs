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
        public static readonly AuditEventType LoginSuccess = new AuditEventType(1001, "Login success");
        public static readonly AuditEventType LoginFailure = new AuditEventType(1002, "Login failure");
        public static readonly AuditEventType<RoleEnum> Unauthorized = new AuditEventType<RoleEnum>(
            1003, "Unauthorized request", (role) => new
            {
                Role = role.ToString(),
            });
        public static readonly AuditEventType Logout = new AuditEventType(1004, "Logout success");
        public static readonly AuditEventType AccountLockByUser = new AuditEventType(1005, "Account lock by user");
        public static readonly AuditEventType UserPasswordChanged = new AuditEventType(1006, "User password changed");
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
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<ApplicationUser, string> UserAccountLockByAdmin = new AuditEventType<ApplicationUser, string>(
            3003, "User account lock by Admin", (user, reason) => new
            {
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<ApplicationUser> UserAccountDeleted = new AuditEventType<ApplicationUser>(
            3004, "User account deleted", (user) => new
            {
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<ApplicationUser> UserAccountEnabled = new AuditEventType<ApplicationUser>(
            3005, "New user account enabled", (user) => new
            {
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<ApplicationUser> PasswordResetRequested = new AuditEventType<ApplicationUser>(
            3006, "Account password reset requested", (user) => new
            {
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<ApplicationUser> PasswordResetCompleted = new AuditEventType<ApplicationUser>(
            3007, "Account password reset completed", (user) => new
            {
                AccountUserName = user.UserName,
            });
        public static readonly AuditEventType<string> PasswordResetRequestedForInvalidEmail = new AuditEventType<string>(
            3008, "Account password reset requested for invalid email", (email) => new
            {
                RequestedEmail = email,
            });
        public static readonly AuditEventType LoginNotAllowed = new AuditEventType(3009, "Login not allowed");
        public static readonly AuditEventType LoginIsLockedOut = new AuditEventType(3010, "Login account is locked out");
        #endregion

        #region Content Access Admin [4000 - 4999]
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
        public static readonly AuditEventType<SelectionGroup, long> SelectionGroupUserAssigned = new AuditEventType<SelectionGroup, long>(
            4003, "User assigned to selection group", (selectionGroup, userId) => new
            {
                SelectionGroupId = selectionGroup.Id,
                UserId = userId,
            });
        public static readonly AuditEventType<SelectionGroup, long> SelectionGroupUserRemoved = new AuditEventType<SelectionGroup, long>(
            4004, "User removed from selection group", (selectionGroup, userId) => new
            {
                SelectionGroupId = selectionGroup.Id,
                UserId = userId,
            });
        public static readonly AuditEventType<SelectionGroup, ContentReductionTask> SelectionChangeReductionQueued = new AuditEventType<SelectionGroup, ContentReductionTask>(
            4005, "Selection change reduction task queued", (selectionGroup, reductionTask) => new
            {
            });
        public static readonly AuditEventType<SelectionGroup, ContentReductionTask> SelectionChangeReductionCanceled = new AuditEventType<SelectionGroup, ContentReductionTask>(
            4006, "Selection change reduction task canceled", (selectionGroup, reductionTask) => new
            {
            });
        public static readonly AuditEventType<SelectionGroup> SelectionChangeMasterAccessGranted = new AuditEventType<SelectionGroup>(
            4007, "Selection group given master access", (selectionGroup) => new
            {
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

        #endregion

        #region Reduction Server [5000 - 5999]
        public static readonly AuditEventType<object> ReductionValidationFailed = new AuditEventType<object>(
            5001, "Reduction Validation Failed", (logObject) => logObject);
        public static readonly AuditEventType<object> HierarchyExtractionSucceeded = new AuditEventType<object>(
            5101, "Content hierarchy extraction completed", (logObject) => logObject);
        public static readonly AuditEventType<object> HierarchyExtractionFailed = new AuditEventType<object>(
            5102, "Content hierarchy extraction failed", (logObject) => logObject);
        public static readonly AuditEventType<object> ContentReductionSucceeded = new AuditEventType<object>(
            5201, "Content reduction completed", (logObject) => logObject);
        public static readonly AuditEventType<object> ContentReductionFailed = new AuditEventType<object>(
            5202, "Content reduction failed", (logObject) => logObject);
        public static readonly AuditEventType<object> PublicationRequestProcessingSuccess = new AuditEventType<object>(
            5301, "Content PublicationRequest Succeeded", (logObject) => logObject);
        #endregion

        #region Content Publishing [6000 - 6999]
        public static readonly AuditEventType<RootContentItem> RootContentItemCreated = new AuditEventType<RootContentItem>(
            6001, "Root content item created", (rootContentItem) => new
            {
            });
        public static readonly AuditEventType<RootContentItem> RootContentItemDeleted = new AuditEventType<RootContentItem>(
            6002, "Root content item deleted", (rootContentItem) => new
            {
            });
        public static readonly AuditEventType<RootContentItem> RootContentItemUpdated = new AuditEventType<RootContentItem>(
            6003, "Root content item updated", (rootContentItem) => new
            {
            });
        public static readonly AuditEventType<RootContentItem, ContentPublicationRequest> PublicationQueued = new AuditEventType<RootContentItem, ContentPublicationRequest>(
            6101, "Publication request queued", (rootContentItem, publicationRequest) => new
            {
            });
        public static readonly AuditEventType<RootContentItem, ContentPublicationRequest> PublicationCanceled = new AuditEventType<RootContentItem, ContentPublicationRequest>(
            6102, "Publication request canceled", (rootContentItem, publicationRequest) => new
            {
            });
        public static readonly AuditEventType<RootContentItem, ContentPublicationRequest> GoLiveValidationFailed = new AuditEventType<RootContentItem, ContentPublicationRequest>(
            6103, "GoLive Validation Failed", (rootContentItem, publicationRequest) => new
            {
            });
        public static readonly AuditEventType ChecksumInvalid = new AuditEventType(6104, "Checksum Invalid");
        public static readonly AuditEventType<RootContentItem, ContentPublicationRequest, string> ContentPublicationGoLive = new AuditEventType<RootContentItem, ContentPublicationRequest, string>(
            6105, "Content publication golive", (rootContentItem, publicationRequest, summaryGUID) => new
            {
                SummaryGUID = summaryGUID,
            });
        public static readonly AuditEventType<object> PreGoLiveSummary = new AuditEventType<object>(
            6106, "Content publication pre-golive summary", (preliveSummary) => preliveSummary);
        public class PreLiveSummaryLogObject
        {
            public long ValidationSummaryId { get; set; }
            public long PublicationRequestId { get; set; }
            public string AttestationLanguage { get; set; }
            public string ContentDescription { get; set; }
            public string RootContentName { get; set; }
            public string ContentTypeName { get; set; }
            public long LiveHierarchy { get; set; }
            public long NewHierarchy { get; set; }
            public bool DoesReduce { get; set; }
            public long ClientName { get; set; }
        }
        public static readonly AuditEventType<RootContentItem, ContentPublicationRequest> ContentPublicationRejected = new AuditEventType<RootContentItem, ContentPublicationRequest>(
            6107, "Content publication rejected", (rootContentItem, publicationRequest) => new
            {
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
        public static readonly AuditEventType<ProfitCenter, ApplicationUser> UserAssignedToProfitCenter = new AuditEventType<ProfitCenter, ApplicationUser>(
            7102, "User assigned to profit center", (profitCenter, user) => new
            {
                ProfitCenterId = profitCenter.Id,
                ProfitCenterName = profitCenter.Name,
                UserId = user.Id,
                UserName = user.UserName,
            });
        public static readonly AuditEventType<ProfitCenter, ApplicationUser> UserRemovedFromProfitCenter = new AuditEventType<ProfitCenter, ApplicationUser>(
            7103, "User removed from profit center", (profitCenter, user) => new
            {
                ProfitCenterId = profitCenter.Id,
                UserId = user.Id,
            });
        #endregion
        #endregion

        #region Common loggable object declarations
        public class SelectionGroupLogObject
        {
            public long ClientId { get; set; }
            public long RootContentItemId { get; set; }
            public long SelectionGroupId { get; set; }
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

}

using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace AuditLogLib.Event
{
    public class AuditEventIdRegistry
    {
        // WARNING!!!  After production begins, never change the numeric ID of any AuditEventId

        #region Uncategorized [0000 - 0999]
        public static readonly AuditEventId Unspecified = new AuditEventId(0001, "Unspecified");
        public static readonly AuditEventId InvalidRequest = new AuditEventId(0002, "Invalid request");
        #endregion

        #region User activity [1000 - 1999]
        public static readonly AuditEventId LoginSuccess = new AuditEventId(1001, "Login success");
        public static readonly AuditEventId LoginFailure = new AuditEventId(1002, "Login failure");
        public static readonly AuditEventId Unauthorized = new AuditEventId(1003, "Unauthorized request");
        public static readonly AuditEventId Logout = new AuditEventId(1004, "Logout success");
        public static readonly AuditEventId AccountLockByUser = new AuditEventId(1005, "Account lock by user");
        public static readonly AuditEventId UserPasswordChanged = new AuditEventId(1006, "User password changed");
        #endregion

        #region Client Admin [2000 - 2999]
        public static readonly AuditEventId<Client, ApplicationUser> UserAssignedToClient = new AuditEventId<Client, ApplicationUser>(
            2001, "User assigned to client", (client, user) => new
            {
            });
        public static readonly AuditEventId<Client, ApplicationUser> UserRemovedFromClient = new AuditEventId<Client, ApplicationUser>(
            2002, "User removed from client", (client, user) => new
            {
            });
        public static readonly AuditEventId<Client> NewClientSaved = new AuditEventId<Client>(
            2003, "New client saved", (client) => new
            {
            });
        public static readonly AuditEventId<Client> ClientEdited = new AuditEventId<Client>(
            2004, "Client edited", (client) => new
            {
            });
        public static readonly AuditEventId<Client> ClientDeleted = new AuditEventId<Client>(
            2005, "Client deleted", (client) => new
            {
            });
        public static readonly AuditEventId<UserRoleInClient> ClientRoleAssigned = new AuditEventId<UserRoleInClient>(
            2006, "Client role assigned", (userClient) => new
            {
            });
        public static readonly AuditEventId<UserRoleInClient> ClientRoleRemoved = new AuditEventId<UserRoleInClient>(
            2007, "Client role removed", (userClient) => new
            {
            });
        #endregion

        #region User Account [3000 - 3999]
        public static readonly AuditEventId<ApplicationUser> UserAccountCreated = new AuditEventId<ApplicationUser>(
            3001, "User account created", (user) => new
            {
            });
        public static readonly AuditEventId<ApplicationUser> UserAccountModified = new AuditEventId<ApplicationUser>(
            3002, "User account modified", (user) => new
            {
            });
        public static readonly AuditEventId<ApplicationUser, string> UserAccountLockByAdmin = new AuditEventId<ApplicationUser, string>(
            3003, "User account lock by Admin", (user, reason) => new
            {
            });
        public static readonly AuditEventId<ApplicationUser> UserAccountDeleted = new AuditEventId<ApplicationUser>(
            3004, "User account deleted", (user) => new
            {
            });
        #endregion

        #region Content Access Admin [4000 - 4999]
        public static readonly AuditEventId<Client, RootContentItem, SelectionGroup> SelectionGroupCreated = new AuditEventId<Client, RootContentItem, SelectionGroup>(
            4001, "Selection group created", (client, rootContentItem, selectionGroup) => new
            {
                ClientId = client.Id,
                RootContentItemId = rootContentItem.Id,
                SelectionGroupId = selectionGroup.Id,
            });
        public static readonly AuditEventId<SelectionGroup> SelectionGroupDeleted = new AuditEventId<SelectionGroup>(
            4002, "Selection group deleted", (selectionGroup) => new SelectionGroupLogTemplate
            {
                ClientId = selectionGroup.RootContentItem?.Client?.Id ?? 0,
                RootContentItemId = selectionGroup.RootContentItem?.Id ?? 0,
                SelectionGroupId = selectionGroup.Id,
            });
        public static readonly AuditEventId<SelectionGroup, ApplicationUser> SelectionGroupUserAssigned = new AuditEventId<SelectionGroup, ApplicationUser>(
            4003, "User assigned to selection group", (selectionGroup, user) => new
            {
                ClientId = selectionGroup.RootContentItem?.Client?.Id ?? 0,
                RootContentItemId = selectionGroup.RootContentItem?.Id ?? 0,
                SelectionGroupId = selectionGroup.Id,
                UserId = user.Id,
            });
        public static readonly AuditEventId<SelectionGroup, ApplicationUser> SelectionGroupUserRemoved = new AuditEventId<SelectionGroup, ApplicationUser>(
            4004, "User removed from selection group", (selectionGroup, user) => new
            {
                ClientId = selectionGroup.RootContentItem?.Client?.Id ?? 0,
                RootContentItemId = selectionGroup.RootContentItem?.Id ?? 0,
                SelectionGroupId = selectionGroup.Id,
                UserId = user.Id,
            });
        public static readonly AuditEventId<SelectionGroup, ContentReductionTask> SelectionChangeReductionQueued = new AuditEventId<SelectionGroup, ContentReductionTask>(
            4005, "Selection change reduction task queued", (selectionGroup, reductionTask) => new
            {
            });
        public static readonly AuditEventId<SelectionGroup, ContentReductionTask> SelectionChangeReductionCanceled = new AuditEventId<SelectionGroup, ContentReductionTask>(
            4006, "Selection change reduction task canceled", (selectionGroup, reductionTask) => new
            {
            });
        public static readonly AuditEventId<SelectionGroup> SelectionChangeMasterAccessGranted = new AuditEventId<SelectionGroup>(
            4007, "Selection group given master access", (selectionGroup) => new
            {
            });
        public static readonly AuditEventId<SelectionGroup, string> SelectionGroupSuspensionUpdate = new AuditEventId<SelectionGroup, string>(
            4008, "Selection group suspension status updated", (selectionGroup, reason) => new
            {
            });
        #endregion

        #region Reduction Server [5000 - 5999]
        public static readonly AuditEventId<ContentReductionTask> ReductionValidationFailed = new AuditEventId<ContentReductionTask>(
            5001, "Reduction Validation Failed", (reductionTask) => new
            {
            });
        public static readonly AuditEventId<ContentReductionTask> HierarchyExtractionSucceeded = new AuditEventId<ContentReductionTask>(
            5101, "Content hierarchy extraction completed", (reductionTask) => new
            {
            });
        public static readonly AuditEventId<ContentReductionTask> HierarchyExtractionFailed = new AuditEventId<ContentReductionTask>(
            5102, "Content hierarchy extraction failed", (reductionTask) => new
            {
            });
        public static readonly AuditEventId<ContentReductionTask> ContentReductionSucceeded = new AuditEventId<ContentReductionTask>(
            5201, "Content reduction completed", (reductionTask) => new
            {
            });
        public static readonly AuditEventId<ContentReductionTask> ContentReductionFailed = new AuditEventId<ContentReductionTask>(
            5202, "Content reduction failed", (reductionTask) => new
            {
            });
        public static readonly AuditEventId<ContentPublicationRequest> PublicationRequestProcessingSuccess = new AuditEventId<ContentPublicationRequest>(
            5301, "Content PublicationRequest Succeeded", (publicationRequest) => new
            {
            });
        #endregion

        #region Content Publishing [6000 - 6999]
        public static readonly AuditEventId<RootContentItem> RootContentItemCreated = new AuditEventId<RootContentItem>(
            6001, "Root content item created", (rootContentItem) => new
            {
            });
        public static readonly AuditEventId<RootContentItem> RootContentItemDeleted = new AuditEventId<RootContentItem>(
            6002, "Root content item deleted", (rootContentItem) => new
            {
            });
        public static readonly AuditEventId<RootContentItem> RootContentItemUpdated = new AuditEventId<RootContentItem>(
            6003, "Root content item updated", (rootContentItem) => new
            {
            });
        public static readonly AuditEventId<RootContentItem, ContentPublicationRequest> PublicationQueued = new AuditEventId<RootContentItem, ContentPublicationRequest>(
            6101, "Publication request queued", (rootContentItem, publicationRequest) => new
            {
            });
        public static readonly AuditEventId<RootContentItem, ContentPublicationRequest> PublicationCanceled = new AuditEventId<RootContentItem, ContentPublicationRequest>(
            6102, "Publication request canceled", (rootContentItem, publicationRequest) => new
            {
            });
        public static readonly AuditEventId<RootContentItem, ContentPublicationRequest> GoLiveValidationFailed = new AuditEventId<RootContentItem, ContentPublicationRequest>(
            6103, "GoLive Validation Failed", (rootContentItem, publicationRequest) => new
            {
            });
        public static readonly AuditEventId ChecksumInvalid = new AuditEventId(6104, "Checksum Invalid");
        public static readonly AuditEventId<RootContentItem, ContentPublicationRequest> ContentPublicationGoLive = new AuditEventId<RootContentItem, ContentPublicationRequest>(
            6105, "Content publication golive", (rootContentItem, publicationRequest) => new
            {
            });
        public static readonly AuditEventId PreGoLiveSummary = new AuditEventId(6106, "Content publication pre-golive summary");
        public static readonly AuditEventId<RootContentItem, ContentPublicationRequest> ContentPublicationRejected = new AuditEventId<RootContentItem, ContentPublicationRequest>(
            6107, "Content publication rejected", (rootContentItem, publicationRequest) => new
            {
            });
        #endregion

        private class SelectionGroupLogTemplate
        {
            public long ClientId { get; set; }
            public long RootContentItemId { get; set; }
            public long SelectionGroupId { get; set; }
        }
    }
}

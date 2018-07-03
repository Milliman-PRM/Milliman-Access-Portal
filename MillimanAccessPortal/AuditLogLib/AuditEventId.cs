using System;
using Microsoft.Extensions.Logging;

namespace AuditLogLib
{
    /// <summary>
    /// Sort of enumeration for argument to the AuditLogger.Log(...) EventId argument
    /// </summary>
    public class AuditEventId
    {
        internal static readonly int AuditEventBaseId = 1000;
        internal static readonly int AuditEventMaxId = AuditEventBaseId + 99999;

        // WARNING!!!  After production begins, never change the numeric ID of any AuditEventId

        // Uncategorized 1 - 999
        public static readonly AuditEventId Unspecified = new AuditEventId(1, "Unspecified");
        public static readonly AuditEventId InvalidRequest = new AuditEventId(2, "Invalid request");

        // User activity category 1000 - 1999
        public static readonly AuditEventId LoginSuccess = new AuditEventId(1001, "Login success");
        public static readonly AuditEventId LoginFailure = new AuditEventId(1002, "Login failure");
        public static readonly AuditEventId Unauthorized = new AuditEventId(1003, "Unauthorized request");
        public static readonly AuditEventId Logout = new AuditEventId(1004, "Logout success");
        public static readonly AuditEventId AccountLockByUser = new AuditEventId(1005, "Account lock by user");
        public static readonly AuditEventId UserPasswordChanged = new AuditEventId(1006, "User password changed");

        // Client Admin category 2000 - 2999
        public static readonly AuditEventId UserAssignedToClient = new AuditEventId(2001, "User assigned to client");
        public static readonly AuditEventId UserRemovedFromClient = new AuditEventId(2002, "User removed from client");
        public static readonly AuditEventId NewClientSaved = new AuditEventId(2003, "New client saved");
        public static readonly AuditEventId ClientEdited = new AuditEventId(2004, "Client edited");
        public static readonly AuditEventId ClientDeleted = new AuditEventId(2005, "Client deleted");
        public static readonly AuditEventId ClientRoleAssigned = new AuditEventId(2006, "Client role assigned");
        public static readonly AuditEventId ClientRoleRemoved = new AuditEventId(2007, "Client role removed");

        // User Admin category 3000 - 3999
        public static readonly AuditEventId UserAccountCreated = new AuditEventId(3001, "User account created");
        public static readonly AuditEventId UserAccountModified = new AuditEventId(3002, "User account modified");
        public static readonly AuditEventId UserAccountLockByAdmin = new AuditEventId(3003, "User account lock by Admin");
        public static readonly AuditEventId UserAccountDeleted = new AuditEventId(3004, "User account deleted");

        // Content Access Admin category 4000 - 4999
        public static readonly AuditEventId SelectionGroupCreated = new AuditEventId(4001, "Selection group created");
        public static readonly AuditEventId SelectionGroupDeleted = new AuditEventId(4002, "Selection group deleted");
        public static readonly AuditEventId SelectionGroupUserAssigned = new AuditEventId(4003, "User assigned to selection group");
        public static readonly AuditEventId SelectionGroupUserRemoved = new AuditEventId(4004, "User removed from selection group");
        public static readonly AuditEventId SelectionChangeReductionQueued = new AuditEventId(4005, "Selection change reduction task queued");
        public static readonly AuditEventId SelectionChangeReductionCanceled = new AuditEventId(4006, "Selection change reduction task canceled");

        // Reduction Server category 5000 - 5999
        public static readonly AuditEventId ReductionValidationFailed = new AuditEventId(5001, "Reduction Validation Failed");
        public static readonly AuditEventId HierarchyExtractionSucceeded = new AuditEventId(5101, "Content hierarchy extraction completed");
        public static readonly AuditEventId HierarchyExtractionFailed = new AuditEventId(5102, "Content hierarchy extraction failed");
        public static readonly AuditEventId ContentReductionSucceeded = new AuditEventId(5201, "Content reduction completed");
        public static readonly AuditEventId ContentReductionFailed = new AuditEventId(5202, "Content reduction failed");

        // Content Publishing category 6000 - 6999
        public static readonly AuditEventId RootContentItemCreated = new AuditEventId(4001, "Root content item created");
        public static readonly AuditEventId RootContentItemDeleted = new AuditEventId(4002, "Root content item deleted");
        public static readonly AuditEventId RootContentItemUpdated = new AuditEventId(4003, "Root content item updated");
        public static readonly AuditEventId PublicationQueued = new AuditEventId(4101, "Publication request queued");
        public static readonly AuditEventId PublicationCanceled = new AuditEventId(4102, "Publication request canceled");
        public static readonly AuditEventId GoLiveValidationFailed = new AuditEventId(4103, "GoLive Validation Failed");
        public static readonly AuditEventId ChecksumInvalid = new AuditEventId(4104, "Checksum Invalid");
        public static readonly AuditEventId ContentPublicationGoLive = new AuditEventId(4105, "Content publication golive");

        public AuditEventId(int id, string name = "")
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }

        public static implicit operator AuditEventId(int i)
        {
            return new AuditEventId(i);
        }


    }
}

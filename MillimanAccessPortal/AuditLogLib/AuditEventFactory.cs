using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace AuditLogLib
{
    public class AuditEventFactory
    {
        public static AuditEventFactory Instance { get; } = new AuditEventFactory();

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

        // User Account category 3000 - 3999
        public static readonly AuditEventId UserAccountCreated = new AuditEventId(3001, "User account created");
        public static readonly AuditEventId UserAccountModified = new AuditEventId(3002, "User account modified");
        public static readonly AuditEventId UserAccountLockByAdmin = new AuditEventId(3003, "User account lock by Admin");
        public static readonly AuditEventId UserAccountDeleted = new AuditEventId(3004, "User account deleted");

        // Content Access Admin category 4000 - 4999
        public static readonly AuditEventId<SelectionGroup> SelectionGroupCreated = new AuditEventId<SelectionGroup>(4001, "Selection group created");
        public static readonly AuditEventId SelectionGroupDeleted = new AuditEventId(4002, "Selection group deleted");
        public static readonly AuditEventId SelectionGroupUserAssigned = new AuditEventId(4003, "User assigned to selection group");
        public static readonly AuditEventId SelectionGroupUserRemoved = new AuditEventId(4004, "User removed from selection group");
        public static readonly AuditEventId SelectionChangeReductionQueued = new AuditEventId(4005, "Selection change reduction task queued");
        public static readonly AuditEventId SelectionChangeReductionCanceled = new AuditEventId(4006, "Selection change reduction task canceled");
        public static readonly AuditEventId SelectionChangeMasterAccessGranted = new AuditEventId(4007, "Selection group given master access");
        public static readonly AuditEventId SelectionGroupSuspensionUpdate = new AuditEventId(4008, "Selection group suspension status updated");

        // Reduction Server category 5000 - 5999
        public static readonly AuditEventId ReductionValidationFailed = new AuditEventId(5001, "Reduction Validation Failed");
        public static readonly AuditEventId HierarchyExtractionSucceeded = new AuditEventId(5101, "Content hierarchy extraction completed");
        public static readonly AuditEventId HierarchyExtractionFailed = new AuditEventId(5102, "Content hierarchy extraction failed");
        public static readonly AuditEventId ContentReductionSucceeded = new AuditEventId(5201, "Content reduction completed");
        public static readonly AuditEventId ContentReductionFailed = new AuditEventId(5202, "Content reduction failed");
        public static readonly AuditEventId PublicationRequestProcessingSuccess = new AuditEventId(5301, "Content PublicationRequest Succeeded");

        // Content Publishing category 6000 - 6999
        public static readonly AuditEventId RootContentItemCreated = new AuditEventId(6001, "Root content item created");
        public static readonly AuditEventId RootContentItemDeleted = new AuditEventId(6002, "Root content item deleted");
        public static readonly AuditEventId RootContentItemUpdated = new AuditEventId(6003, "Root content item updated");
        public static readonly AuditEventId PublicationQueued = new AuditEventId(6101, "Publication request queued");
        public static readonly AuditEventId PublicationCanceled = new AuditEventId(6102, "Publication request canceled");
        public static readonly AuditEventId GoLiveValidationFailed = new AuditEventId(6103, "GoLive Validation Failed");
        public static readonly AuditEventId ChecksumInvalid = new AuditEventId(6104, "Checksum Invalid");
        public static readonly AuditEventId ContentPublicationGoLive = new AuditEventId(6105, "Content publication golive");
        public static readonly AuditEventId PreGoLiveSummary = new AuditEventId(6106, "Content publication pre-golive summary");
        public static readonly AuditEventId ContentPublicationRejected = new AuditEventId(6107, "Content publication rejected");

        private readonly Dictionary<AuditEventId, Object> _registeredEvents = new Dictionary<AuditEventId, Object>();

        private AuditEventFactory()
        {
        }

        public void Register(AuditEventId auditEventId)
        {
            
        }

        public void Register<T>(AuditEventId<T> auditEventId, Func<T, Object> func)
        {
            _registeredEvents.Add(auditEventId, func);
        }

        public void Register<T, U>(AuditEventId<T, U> auditEventId, Func<T, U, Object> func)
        {

        }

        public AuditEvent Create<T>(AuditEventId eventId, T entity, string userName = "System", string sessionId = "", [CallerMemberName] string callerName = "")
        {
            var registration = _registeredEvents.SingleOrDefault(e => e.Key.id == eventId.id);

            if (registration.Equals(default(KeyValuePair<AuditEventId, Object>)))
            {
                // return generic "Unconfigured audit event" object, or throw
                return null;
            }

            var objectFn = registration.Value as Func<T, Object>;
            var loggableObject = objectFn(entity);

            return new AuditEvent
            {
                TimeStampUtc = DateTime.Now,
                EventType = registration.Key.Name,
                EventDataObject = loggableObject,
                User = userName,
                SessionId = sessionId,
                Source = callerName,
            };
        }
    }
}

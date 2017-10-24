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
        internal static readonly int AuditEventMaxId = AuditEventBaseId + 9999;

        // These are the members for use by users of AuditLogger.Log()

        // ClientAdmin related 1001+
        public static readonly AuditEventId Unspecified = new AuditEventId(AuditEventBaseId, "Unspecified");
        public static readonly AuditEventId LoginSuccess = new AuditEventId(AuditEventBaseId + 1, "Login success");
        public static readonly AuditEventId LoginFailure = new AuditEventId(AuditEventBaseId + 2, "Login failure");
        public static readonly AuditEventId Unauthorized = new AuditEventId(AuditEventBaseId + 3, "Unauthorized request");
        public static readonly AuditEventId Logout = new AuditEventId(AuditEventBaseId + 4, "Logout success");

        // ClientAdmin related 1101+
        public static readonly AuditEventId UserAssignedToClient = new AuditEventId(AuditEventBaseId + 101, "User assigned To Client");
        public static readonly AuditEventId UserRemovedFromClient = new AuditEventId(AuditEventBaseId + 102, "User removed From Client");
        public static readonly AuditEventId NewClientSaved = new AuditEventId(AuditEventBaseId + 103, "New client saved");
        public static readonly AuditEventId ClientEdited = new AuditEventId(AuditEventBaseId + 104, "Client edited");
        public static readonly AuditEventId ClientDeleted = new AuditEventId(AuditEventBaseId + 105, "Client deleted");
        public static readonly AuditEventId ClientRoleAssigned = new AuditEventId(AuditEventBaseId + 106, "Client role assigned");
        public static readonly AuditEventId ClientRoleRemoved = new AuditEventId(AuditEventBaseId + 107, "Client role removed");

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

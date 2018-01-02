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

        // Unspecified
        public static readonly AuditEventId Unspecified = new AuditEventId(AuditEventBaseId, "Unspecified");

        // User activity category 1000 - 1999
        public static readonly AuditEventId LoginSuccess = new AuditEventId(1001, "Login success");
        public static readonly AuditEventId LoginFailure = new AuditEventId(1002, "Login failure");
        public static readonly AuditEventId Unauthorized = new AuditEventId(1003, "Unauthorized request");
        public static readonly AuditEventId Logout = new AuditEventId(1004, "Logout success");
        public static readonly AuditEventId AccountLockByUser = new AuditEventId(1005, "Account lock by user");
        public static readonly AuditEventId UserPasswordChanged = new AuditEventId(1006, "User password changed");

        // Client Admin category 2000 - 2999
        public static readonly AuditEventId UserAssignedToClient = new AuditEventId(2001, "User assigned To Client");
        public static readonly AuditEventId UserRemovedFromClient = new AuditEventId(2002, "User removed From Client");
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

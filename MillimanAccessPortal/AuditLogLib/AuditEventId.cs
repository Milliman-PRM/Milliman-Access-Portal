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
        public static readonly AuditEventId LoginSuccess = new AuditEventId(AuditEventBaseId + 1, "Login Success");
        public static readonly AuditEventId LoginFailure = new AuditEventId(AuditEventBaseId + 2, "Login Failure");
        public static readonly AuditEventId Unauthorized = new AuditEventId(AuditEventBaseId + 3, "Unauthorized Request");

        // ClientAdmin related 1101+
        public static readonly AuditEventId UserAssignedToClient = new AuditEventId(AuditEventBaseId + 101, "User Assigned To Client");

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

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
        internal static readonly int AuditEventMaxId = AuditEventBaseId + 999;

        // These are the members for use by users of AuditLogger.Log()
        public static readonly EventId Unspecified = CreateNew(AuditEventBaseId, "Unspecified");
        public static readonly EventId LoginSuccess = CreateNew(AuditEventBaseId + 1, "Login Success");
        public static readonly EventId LoginFailure = CreateNew(AuditEventBaseId + 2, "Login Failure");


        /// <summary>
        /// Internal convenience method to initialize members with bounds checking on Id
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        private static EventId CreateNew(int Id, string Name)
        {          
            if (Id < AuditEventBaseId || Id > AuditEventMaxId)
            {
                throw new ArgumentOutOfRangeException("Tried to create an AuditEventId object with invalid Id");
            }

            return new EventId(Id, Name);
        }
    }
}

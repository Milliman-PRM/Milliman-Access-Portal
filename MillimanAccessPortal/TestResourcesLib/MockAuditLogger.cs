/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: Mocked version of AuditLogger class enabling unit tests with no dependency on a real audit logging database.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using AuditLogLib.Services;
using Moq;

namespace TestResourcesLib
{
    public static class MockAuditLogger
    {
        public static Mock<IAuditLogger> New()
        {
            //AuditLogger.Config = new AuditLoggerConfiguration { AuditLogConnectionString = "" };
            Mock<IAuditLogger> ReturnObject = new Mock<IAuditLogger>();
            ReturnObject.Setup(al => al.Log(It.IsAny<AuditEvent>(), It.IsAny<string>())).Callback(() => { /*Do nothing*/});
            ReturnObject.Setup(al => al.Log(It.IsAny<AuditEvent>())).Callback(() => { /*Do nothing*/});

            return ReturnObject;
        }

    }
}

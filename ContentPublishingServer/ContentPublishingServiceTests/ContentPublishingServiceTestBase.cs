/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using ContentPublishingLib;

namespace ContentPublishingServiceTests
{
    public class ContentPublishingServiceTestBase
    {
        public DateTime StartTime = DateTime.MaxValue;

        public ContentPublishingServiceTestBase()
        {
            Configuration.LoadConfiguration();
            StartTime = DateTime.UtcNow;
        }
    }
}

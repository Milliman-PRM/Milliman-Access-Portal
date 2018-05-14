/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using ContentPublishingLib;

namespace ContentPublishingServiceTests
{
    public class ContentReductionServiceTestBase
    {
        public DateTime StartTime = DateTime.MaxValue;

        public ContentReductionServiceTestBase()
        {
            Configuration.LoadConfiguration();
            StartTime = DateTime.UtcNow;
        }
    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Allows the documentation of any xunit test by placing this attribute on the test method.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Serilog;
using System.Reflection;
using Xunit.Sdk;

namespace TestResourcesLib
{
    public class LogTestBeginEndAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            Log.Information($"LogTestBeginEnd: Starting test {methodUnderTest.DeclaringType}.{methodUnderTest.Name}");
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Log.Information($"LogTestBeginEnd: Ending test {methodUnderTest.DeclaringType}.{methodUnderTest.Name}");
        }
    }
}

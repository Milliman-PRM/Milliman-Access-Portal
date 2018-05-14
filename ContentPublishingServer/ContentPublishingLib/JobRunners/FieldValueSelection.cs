/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents the selection status of a specific field value
 * DEVELOPER NOTES: <What future developers need to know.>
 */

namespace ContentPublishingLib.JobRunners
{
    /// <summary>
    /// This type is public because it is serialized to Json and the serializer needs full access
    /// </summary>
    public class FieldValueSelection
    {
        public string FieldName;
        public string FieldValue;
        public bool Selected;
    }
}

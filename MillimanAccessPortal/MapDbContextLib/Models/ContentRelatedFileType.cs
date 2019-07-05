/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Declare enumerated types for content related files
 * DEVELOPER NOTES: <What future developers need to know.>
 */

namespace MapDbContextLib.Models
{
    public enum ContentRelatedFileType
    {
        Unknown = 0,
        Pdf = 1,
        Html = 2,
        FileDownload = 3,
    }
}

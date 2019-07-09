/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Declare enumerated types for content associated files
 * DEVELOPER NOTES: <What future developers need to know.>
 */


namespace MapDbContextLib.Models
{
    public enum ContentAssociatedFileType
    {
        Unknown = 0,
        Pdf = 1,
        Html = 2,
        FileDownload = 3,
    }
}

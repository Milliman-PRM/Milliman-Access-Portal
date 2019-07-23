/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Declare enumerated types for content associated files
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace MapDbContextLib.Models
{
    public enum ContentAssociatedFileType
    {
        [Display(Name = "Unknown")]
        [StringList(StringArray = new string[0])]
        Unknown = 0,

        [Display(Name = "PDF")]
        [StringList(StringArray = new string[] {"pdf"})]
        Pdf = 1,

        [Display(Name = "HTML")]
        [StringList(StringArray = new string[] { "htm", "html" })]
        Html = 2,

        [Display(Name = "File Download")]
        [StringList(StringArray = new string[] { "*" })]
        FileDownload = 3,
    }
}

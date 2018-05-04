/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDbContextLib.Models
{
    public class PublishRequest
    {
        public long RootContentItemId { get; set; }

        public ContentRelatedFile[] RelatedFiles { get; set; }
    }

    public class ContentRelatedFile
    {
        public string FilePurpose { get; set; }

        public Guid FileUploadId { get; set; }
    }
}

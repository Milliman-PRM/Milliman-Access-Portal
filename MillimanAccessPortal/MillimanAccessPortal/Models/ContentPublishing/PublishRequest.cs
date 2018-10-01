/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PublishRequest
    {
        public Guid RootContentItemId { get; set; }

        [Required]
        public UploadedRelatedFile[] RelatedFiles { get; set; }
    }
}

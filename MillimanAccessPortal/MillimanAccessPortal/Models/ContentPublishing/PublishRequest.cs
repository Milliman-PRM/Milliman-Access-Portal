/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: A model for parameters of a ContentPublishing.Publish action request
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PublishRequest
    {
        public Guid RootContentItemId { get; set; }

        [Required]
        public List<UploadedRelatedFile> NewRelatedFiles { get; set; } = new List<UploadedRelatedFile>();

        [Required]
        public List<AssociatedFileModel> AssociatedFiles { get; set; } = new List<AssociatedFileModel>();

        [Required]
        public string[] DeleteFilePurposes { get; set; } = new string[0];

        public JObject TypeSpecificPublishingDetail { get; set; }
    }
}

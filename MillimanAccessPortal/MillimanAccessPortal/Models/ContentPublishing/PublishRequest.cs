/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: A model for parameters of a ContentPublishing.Publish action request
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
        public UploadedRelatedFile[] NewRelatedFiles { get; set; } = new UploadedRelatedFile[0];

        [Required]
        public string[] DeleteFilePurposes { get; set; } = new string[0];
    }
}

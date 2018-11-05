/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a group of users who share common hierarchy selections applicable to one RootContentItem
 * DEVELOPER NOTES: 
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.IO;

namespace MapDbContextLib.Context
{
    public class SelectionGroup
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string GroupName { get; set; }

        public string ContentInstanceUrl { get; set; }

        [ForeignKey("RootContentItem")]
        public Guid RootContentItemId { get; set; }
        public RootContentItem RootContentItem { get; set; }

        /// <summary>
        /// This can't be a foreign key due to use of collection type
        /// </summary>
        public Guid[] SelectedHierarchyFieldValueList { get; set; }

        [Required]
        public bool IsMaster { get; set; }

        [Required]
        public bool IsSuspended { get; set; }

        public string ReducedContentChecksum { get; set; }

        /// <summary>
        /// ContentType specific assignment of Url field to this SelectionGroup instance
        /// Requires this' navigation properties RootContentItem and RootContentItem.ContentType
        /// </summary>
        /// <param name="fileName"></param>
        public void SetContentUrl(string fileName)
        {
            if (RootContentItem == null || RootContentItem.ContentType == null)
            {
                ContentInstanceUrl = null;
                throw new ApplicationException("SelectionGroup.SetContentUrl called without required navigation properties");
            }

            switch (RootContentItem.ContentType.TypeEnum)
            {
                case ContentTypeEnum.Qlikview:
                case ContentTypeEnum.Html:
                case ContentTypeEnum.Pdf:
                case ContentTypeEnum.FileDownload:
                    ContentInstanceUrl = Path.Combine($"{RootContentItem.Id}", fileName);
                    return;

                default:
                    ContentInstanceUrl = null;
                    throw new ApplicationException($"SelectionGroup.SetContentUrl called with unsupported ContentType {RootContentItem.ContentType.TypeEnum.ToString()}");
            }
        }

    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a group of users who share common hierarchy selections applicable to one RootContentItem
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class ContentInstance
    {
        public long Id { get; set; }

        public string Url { get; set; }

        [ForeignKey("ContentItemUserGroup")]
        public long ContentItemUserGroupId { get; set; }
        public ContentItemUserGroup ContentItemUserGroup { get; set; }

        [ForeignKey("RootContentItem")]
        public long RootContentItemId { get; set; }
        public RootContentItem RootContentItem { get; set; }

    }
}

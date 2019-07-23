/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Page level model of constant globals
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Models;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PublishingPageGlobalModel
    {
        public List<ContentTypeNormalized> ContentTypes { get; set; }
        public List<AssociatedFileType> ContentAssociatedFileTypes { get; set; }
    }

    public class AssociatedFileType
    {
        public List<string> FileExtensions { get; set; }
        public string Name { get; set; }
        public ContentAssociatedFileType typeEnum { get; set; }

        public static explicit operator AssociatedFileType(ContentAssociatedFileType typeEnum)
        {
            return new AssociatedFileType
            {
                Name = typeEnum.GetDisplayValueString(),
                FileExtensions = typeEnum.GetStringList(),
                typeEnum = typeEnum,
            };
        }
    }
}

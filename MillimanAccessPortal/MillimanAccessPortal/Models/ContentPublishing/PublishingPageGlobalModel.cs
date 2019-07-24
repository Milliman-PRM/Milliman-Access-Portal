/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Page level model of constant globals
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PublishingPageGlobalModel
    {
        public List<ContentTypeNormalized> ContentTypes { get; set; }
        public List<AssociatedFileType> ContentAssociatedFileTypes { get; set; }

        internal static PublishingPageGlobalModel Build(ApplicationDbContext dbContext)
        {
            var typeValues = Enum.GetValues(typeof(ContentAssociatedFileType)).Cast<ContentAssociatedFileType>();
            return new PublishingPageGlobalModel
            {
                ContentAssociatedFileTypes = typeValues.Select(t => new AssociatedFileType { typeEnum = t, FileExtensions = t.GetStringList(), Name = t.GetDisplayValueString() }).ToList(),
                ContentTypes = dbContext.ContentType.Select(t => new ContentTypeNormalized(t)).ToList(),
            };
        }
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

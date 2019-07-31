/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Page level model of constant globals
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PublishingPageGlobalModel
    {
        public Dictionary<Guid,ContentTypeNormalized> ContentTypes { get; set; }
        public Dictionary<int,AssociatedFileType> ContentAssociatedFileTypes { get; set; }

        internal static PublishingPageGlobalModel Build(ApplicationDbContext dbContext)
        {
            var typeValues = Enum.GetValues(typeof(ContentAssociatedFileType)).Cast<ContentAssociatedFileType>();
            return new PublishingPageGlobalModel
            {
                ContentAssociatedFileTypes = typeValues
                    .Select(t => new AssociatedFileType { TypeEnum = t, FileExtensions = t.GetStringList(StringListKey.FileExtensions), Name = t.GetDisplayValueString() })
                    .ToDictionary(t => (int)t.TypeEnum),
                ContentTypes = dbContext.ContentType
                    .Select(t => new ContentTypeNormalized(t))
                    .ToDictionary(t => t.Id),
            };
        }
    }

    public class AssociatedFileType
    {
        public List<string> FileExtensions { get; set; }
        public string Name { get; set; }
        public ContentAssociatedFileType TypeEnum { get; set; }

        public static explicit operator AssociatedFileType(ContentAssociatedFileType typeEnum)
        {
            return new AssociatedFileType
            {
                Name = typeEnum.GetDisplayValueString(),
                FileExtensions = typeEnum.GetStringList(StringListKey.FileExtensions),
                TypeEnum = typeEnum,
            };
        }
    }
}

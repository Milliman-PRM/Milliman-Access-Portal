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
        public Dictionary<int,AssociatedFileTypeModel> ContentAssociatedFileTypes { get; set; }

    }

    public class AssociatedFileTypeModel
    {
        public List<string> FileExtensions { get; set; }
        public string Name { get; set; }
        public ContentAssociatedFileType TypeEnum { get; set; }

        public static explicit operator AssociatedFileTypeModel(ContentAssociatedFileType typeEnum)
        {
            return new AssociatedFileTypeModel
            {
                Name = typeEnum.GetDisplayValueString(),
                FileExtensions = typeEnum.GetStringList(StringListKey.FileExtensions),
                TypeEnum = typeEnum,
            };
        }
    }
}

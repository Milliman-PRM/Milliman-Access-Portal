/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Page level model of constant globals
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Models;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class PublishingPageGlobalModel
    {
        public Dictionary<Guid,BasicContentType> ContentTypes { get; set; }
        public Dictionary<int,AssociatedFileTypeModel> ContentAssociatedFileTypes { get; set; }

    }

    public class AssociatedFileTypeModel
    {
        public List<string> FileExtensions { get; set; }
        public string DisplayName { get; set; }
        public ContentAssociatedFileType TypeEnum { get; set; }

        public AssociatedFileTypeModel(ContentAssociatedFileType typeEnum)
        {
            DisplayName = typeEnum.GetDisplayNameString();
            FileExtensions = typeEnum.GetStringList(StringListKey.FileExtensions);
            TypeEnum = typeEnum;
        }
    }
}

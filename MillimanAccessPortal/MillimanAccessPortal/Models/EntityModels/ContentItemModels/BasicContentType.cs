/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.EntityModels.ContentItemModels
{
    /// <summary>
    /// A simplified representation of a ContentType to satisfy front end needs.
    /// </summary>
    public class BasicContentType
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public bool CanReduce { get; set; }
        public List<string> FileExtensions { get; set; }
        public ContentTypeEnum TypeEnum { get; set; } = ContentTypeEnum.Unknown;

        public BasicContentType(ContentType t)
        {
            Id = t.Id;
            TypeEnum = t.TypeEnum;
            CanReduce = t.CanReduce;
            FileExtensions = t.FileExtensions;
            DisplayName = t.TypeEnum.GetDisplayValueString();
        }

    }
}

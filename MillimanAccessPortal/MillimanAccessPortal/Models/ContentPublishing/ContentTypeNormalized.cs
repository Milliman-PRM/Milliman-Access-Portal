using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class ContentTypeNormalized
    {
        public Guid Id { get; set; }
        public ContentTypeEnum TypeEnum { get; set; } = ContentTypeEnum.Unknown;
        public string Name { get => ContentType.ContentTypeString[TypeEnum]; }
        public bool CanReduce { get; set; }
        public string DefaultIconName { get; set; }
        public string[] FileExtensions { get; set; } = new string[0];

        public static explicit operator ContentTypeNormalized(ContentType type)
        {
            if (type == null)
            {
                return null;
            }

            return new ContentTypeNormalized
            {
                Id = type.Id,
                TypeEnum = type.TypeEnum,
                CanReduce = type.CanReduce,
                DefaultIconName = type.DefaultIconName,
                FileExtensions = type.FileExtensions,
            };
        }
    }
}

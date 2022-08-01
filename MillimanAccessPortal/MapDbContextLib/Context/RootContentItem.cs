/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a root content item in the persistence data model
 * DEVELOPER NOTES: 
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using MapDbContextLib.Models;
using System.Text.Json;

namespace MapDbContextLib.Context
{
    public class RootContentItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string ContentName { get; set; }

        [ForeignKey("ContentType")]
        public Guid ContentTypeId { get; set; }
        public ContentType ContentType { get; set; }

        [ForeignKey("Client")]
        public Guid ClientId { get; set; }
        public Client Client { get; set; }

        [Required]
        public bool DoesReduce { get; set; }

        [Column(TypeName ="jsonb")]
        // [Required] This causes a problem with migration database update
        public string TypeSpecificDetail { get; set; }

        /// <summary>
        /// If this instance does not have ContentType navigation property populated then this property is treated as null
        /// </summary>
        [NotMapped]
        public TypeSpecificContentItemProperties TypeSpecificDetailObject
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TypeSpecificDetail))
                {
                    return null;
                }
                switch (ContentType?.TypeEnum)
                {
                    case ContentTypeEnum.PowerBi:
                        return JsonSerializer.Deserialize<PowerBiContentItemProperties>(TypeSpecificDetail);

                    case ContentTypeEnum.ContainerApp:
                        return JsonSerializer.Deserialize<ContainerizedAppContentItemProperties>(TypeSpecificDetail);

                    case ContentTypeEnum.Qlikview:
                    case ContentTypeEnum.Pdf:
                    case ContentTypeEnum.Html:
                    case ContentTypeEnum.FileDownload:
                    default:
                        return null;
                }
            }
            set
            {
                switch (ContentType?.TypeEnum)
                {
                    case ContentTypeEnum.PowerBi:
                        TypeSpecificDetail = JsonSerializer.Serialize(value as PowerBiContentItemProperties);
                        break;
                    case ContentTypeEnum.ContainerApp:
                        TypeSpecificDetail = JsonSerializer.Serialize(value as ContainerizedAppContentItemProperties);
                        break;
                    case ContentTypeEnum.Qlikview:
                    case ContentTypeEnum.Pdf:
                    case ContentTypeEnum.Html:
                    case ContentTypeEnum.FileDownload:
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Returns the Type of the TypeSpecificDetailObject property, which depends on the ContentType navigation property
        /// </summary>
        [NotMapped]
        public Type TypeSpecificDetailObjectType
        {
            get
            {
                switch (ContentType?.TypeEnum ?? ContentTypeEnum.Unknown)
                {
                    case ContentTypeEnum.PowerBi:
                        return typeof(PowerBiContentItemProperties);

                    case ContentTypeEnum.ContainerApp:
                        return typeof(ContainerizedAppContentItemProperties);

                    case ContentTypeEnum.Qlikview:
                    case ContentTypeEnum.Pdf:
                    case ContentTypeEnum.Html:
                    case ContentTypeEnum.FileDownload:
                    default:
                        return null;
                }
            }
        }

        public string Description { get; set; }

        public string Notes { get; set; }

        public bool IsSuspended { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Content Disclaimer")]
        public string ContentDisclaimer { get; set; }

        public bool ContentDisclaimerAlwaysShown { get; set; } = false;

        [Column(TypeName = "jsonb")]
        public string ContentFiles { get; set; } = "[]";

        [NotMapped]
        public List<ContentRelatedFile> ContentFilesList
        {
            get
            {
                return ContentFiles == null
                    ? new List<ContentRelatedFile>()
                    : JsonSerializer.Deserialize<List<ContentRelatedFile>>(ContentFiles);
            }
            set
            {
                ContentFiles = value == null
                    ? "[]"
                    : JsonSerializer.Serialize(value);
            }
        }

        [Column(TypeName = "jsonb")]
        public string AssociatedFiles { get; set; } = "[]";

        [NotMapped]
        public List<ContentAssociatedFile> AssociatedFilesList
        {
            get
            {
                return string.IsNullOrEmpty(AssociatedFiles)
                    ? new List<ContentAssociatedFile>()
                    : JsonSerializer.Deserialize<List<ContentAssociatedFile>>(AssociatedFiles);
            }
            set
            {
                AssociatedFiles = value == null
                    ? "[]"
                    : JsonSerializer.Serialize(value);
            }
        }

        [NotMapped]
        public string AcrRepoositoryName => Id.ToString("D").ToLower();
    }

    /// <summary>
    /// Comparer for the above entity class, used to ensure proper operation of `.distinct()` function
    /// </summary>
    public class RootContentItemComparer : IEqualityComparer<RootContentItem>
    {
        public bool Equals(RootContentItem x, RootContentItem y)
        {
            if (x == null && y == null)
                return true;
            else if (x == null || y == null)
                return false;
            else
                return x.Id == y.Id;
        }

        public int GetHashCode(RootContentItem obj)
        {
            return obj.Id.ToString().GetHashCode();
        }
    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a group of users who share common hierarchy selections applicable to one RootContentItem
 * DEVELOPER NOTES: 
 */

using MapDbContextLib.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace MapDbContextLib.Context
{
    public class SelectionGroup
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string GroupName { get; set; }

        /// <summary>
        /// null value indicates inactive selection group due to an error e.g. reduction
        /// </summary>
        public string ContentInstanceUrl { get; set; }

        [ForeignKey("RootContentItem")]
        public Guid RootContentItemId { get; set; }
        public RootContentItem RootContentItem { get; set; }

        /// <summary>
        /// This can't be a foreign key due to use of collection type
        /// </summary>
        public List<Guid> SelectedHierarchyFieldValueList { get; set; }

        [Required]
        public bool IsMaster { get; set; }

        [Required]
        public bool IsSuspended { get; set; }

        [Column(TypeName = "jsonb")]
        // [Required] This causes a problem with migration database update
        public string TypeSpecificDetail { get; set; }

        /// <summary>
        /// If this instance does not have RootContentItem with ContentType navigation property populated then this property is treated as null
        /// </summary>
        [NotMapped]
        public TypeSpecificSelectionGroupProperties TypeSpecificDetailObject
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TypeSpecificDetail))
                {
                    return null;
                }
                switch (RootContentItem?.ContentType?.TypeEnum)
                {
                    case ContentTypeEnum.PowerBi:
                        return JsonConvert.DeserializeObject<PowerBiSelectionGroupProperties>(TypeSpecificDetail, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include });

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
                switch (RootContentItem?.ContentType?.TypeEnum)
                {
                    case ContentTypeEnum.PowerBi:
                        TypeSpecificDetail = JsonConvert.SerializeObject(value as PowerBiSelectionGroupProperties);
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
        /// Returns the Type of the TypeSpecificDetailObject property, which depends on the RootContentItem.ContentType navigation property
        /// </summary>
        [NotMapped]
        public Type TypeSpecificDetailObjectType
        {
            get
            {
                switch (RootContentItem?.ContentType?.TypeEnum ?? ContentTypeEnum.Unknown)
                {
                    case ContentTypeEnum.PowerBi:
                        return typeof(PowerBiSelectionGroupProperties);

                    case ContentTypeEnum.Qlikview:
                    case ContentTypeEnum.Pdf:
                    case ContentTypeEnum.Html:
                    case ContentTypeEnum.FileDownload:
                    default:
                        return null;
                }
            }
        }

        [NotMapped]
        public bool IsInactive { get => string.IsNullOrWhiteSpace(ContentInstanceUrl); }

        [NotMapped]
        public bool IsEditablePowerBiEligible { get => RootContentItem.ContentType.TypeEnum == ContentTypeEnum.PowerBi && (RootContentItem.TypeSpecificDetailObject as PowerBiContentItemProperties).EditableEnabled; }

        [NotMapped]
        public bool Editable
        {
            get
            {
                switch (RootContentItem?.ContentType?.TypeEnum ?? ContentTypeEnum.Unknown)
                {
                    case ContentTypeEnum.PowerBi:
                        return IsEditablePowerBiEligible &&
                               (TypeSpecificDetailObject as PowerBiSelectionGroupProperties).Editable;
                    case ContentTypeEnum.Qlikview:
                    case ContentTypeEnum.Pdf:
                    case ContentTypeEnum.Html:
                    case ContentTypeEnum.FileDownload:
                    default:
                        return false;
                }
            }
        }


        public string ReducedContentChecksum { get; set; }

        /// <summary>
        /// ContentType specific assignment of Url field to this SelectionGroup instance
        /// Requires this instance's navigation properties RootContentItem and RootContentItem.ContentType to be already set and tracked
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

                case ContentTypeEnum.PowerBi:
                    PowerBiContentItemProperties props = RootContentItem?.TypeSpecificDetailObject as PowerBiContentItemProperties;
                    ContentInstanceUrl = props?.LiveReportId ?? Guid.Empty.ToString();
                    return;

                default:
                    ContentInstanceUrl = null;
                    throw new ApplicationException($"SelectionGroup.SetContentUrl called with unsupported ContentType {RootContentItem.ContentType.TypeEnum.ToString()}");
            }
        }

    }
}

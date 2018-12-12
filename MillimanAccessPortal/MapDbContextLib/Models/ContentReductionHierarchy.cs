/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A model representing full hierarchy information used to perform content reduction. 
 * DEVELOPER NOTES: 
 */

using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapDbContextLib.Models
{
    public class ContentReductionHierarchy<T> where T : ReductionFieldValue
    {
        public ContentReductionHierarchy()
        {}

        public static void Test(string JsonString = "")
        {
            if (JsonString == string.Empty)
            {
                JsonString = File.ReadAllText(@"C:\Users\Tom.Puckett\Desktop\testHierarchy.json");
            }
            ContentReductionHierarchy<ReductionFieldValue> ObjFromJson = ContentReductionHierarchy<ReductionFieldValue>.DeserializeJson(JsonString);
            string str = ObjFromJson.SerializeJson();
        }

        public List<ReductionField<T>> Fields { get; set; } = new List<ReductionField<T>>();

        public Guid RootContentItemId { get; set; }

        public static ContentReductionHierarchy<T> DeserializeJson(string JsonString)
        {
            try  // Can fail e.g. if structureType field value does not match an enumeration value name
            {
                return JsonConvert.DeserializeObject<ContentReductionHierarchy<T>>(JsonString);
            }
            catch
            {
                return null;
            }
        }

        public string SerializeJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public void Sort()
        {
            Fields = Fields.OrderBy(hf => hf.DisplayName).ToList();
            foreach (var field in Fields)
            {
                field.Values = field.Values.OrderBy(hfv => hfv.Value).ToList();
            }
        }

        /// <summary>Build hierarchy of selections for a selection group</summary>
        /// <remarks>This builds a nested data structure to represent a hierarchy from several flat tables.</remarks>
        /// <param name="SelectionGroupId">The selection group whose selections are to be gathered</param>
        /// <param name="Selections">Any changes to the current selections to effect in the returned hierarchy</param>
        /// <returns>ContentReductionHierarchy</returns>
        public static ContentReductionHierarchy<ReductionFieldValueSelection> GetFieldSelectionsForSelectionGroup(ApplicationDbContext DbContext, Guid SelectionGroupId, Guid[] Selections = null)
        {
            SelectionGroup SelectionGroup = DbContext.SelectionGroup
                .Include(sg => sg.RootContentItem)
                    .ThenInclude(rci => rci.ContentType)
                .SingleOrDefault(sg => sg.Id == SelectionGroupId);
            if (SelectionGroup == null)
            {
                return null;
            }

            // Apply selection updates if provided
            var SelectedHierarchyFieldValues = Selections ?? SelectionGroup.SelectedHierarchyFieldValueList;

            ContentReductionHierarchy<ReductionFieldValueSelection> ContentReductionHierarchy = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = SelectionGroup.RootContentItemId,
            };

            var RelatedHierarchyFields = DbContext.HierarchyField
                .Where(hf => hf.RootContentItemId == SelectionGroup.RootContentItemId)
                .OrderBy(hf => hf.FieldDisplayName);
            foreach (var HierarchyField in RelatedHierarchyFields)
            {
                var HierarchyFieldValues = DbContext.HierarchyFieldValue
                    .Where(hfv => hfv.HierarchyFieldId == HierarchyField.Id)
                    .OrderBy(hfv => hfv.Value)
                    .Select(hfv => new ReductionFieldValueSelection
                    {
                        Id = hfv.Id,
                        Value = hfv.Value,
                        SelectionStatus = SelectedHierarchyFieldValues.Contains(hfv.Id),
                    })
                    .ToList();

                ReductionField<ReductionFieldValueSelection> ReductionField;
                switch (SelectionGroup.RootContentItem.ContentType.TypeEnum)
                {
                    case ContentTypeEnum.Qlikview:
                        ReductionField = new ReductionField<ReductionFieldValueSelection>
                        {
                            Id = HierarchyField.Id,
                            FieldName = HierarchyField.FieldName,
                            DisplayName = HierarchyField.FieldDisplayName,
                            ValueDelimiter = HierarchyField.FieldDelimiter,
                            StructureType = HierarchyField.StructureType,
                            Values = HierarchyFieldValues,
                        };
                        break;

                    case ContentTypeEnum.Html:
                    case ContentTypeEnum.Pdf:
                    case ContentTypeEnum.FileDownload:
                    default:
                        // Should never get here because RelatedHierarchyFields should be empty for non-reducible types
                        ReductionField = null;
                        break;
                }

                ContentReductionHierarchy.Fields.Add(ReductionField);
            }

            return ContentReductionHierarchy;
        }

        /// <summary>Build hierarchy of a currently live RootContentItem</summary>
        /// <param name="DbContext"></param>
        /// <param name="RootContentItemId">The </param>
        /// <returns>ContentReductionHierarchy</returns>
        public static ContentReductionHierarchy<ReductionFieldValue> GetHierarchyForRootContentItem(ApplicationDbContext DbContext, Guid RootContentItemId)
        {
            RootContentItem ContentItem = DbContext.RootContentItem
                                                     .Include(rc => rc.ContentType)
                                                     .SingleOrDefault(rc => rc.Id == RootContentItemId);
            if (ContentItem == null)
            {
                return null;
            }

            try
            {
                ContentReductionHierarchy<ReductionFieldValue> ReturnObject = new ContentReductionHierarchy<ReductionFieldValue> { RootContentItemId = RootContentItemId };

                foreach (HierarchyField Field in DbContext.HierarchyField
                    .Where(hf => hf.RootContentItemId == RootContentItemId)
                    .OrderBy(hf => hf.FieldDisplayName)
                    .ToList())
                {
                    // There may be different handling required for some future content type. If so, move
                    // the characteristics specific to Qlikview into a class derived from ReductionFieldBase
                    switch (ContentItem.ContentType.TypeEnum)
                    {
                        case ContentTypeEnum.Qlikview:
                            ReturnObject.Fields.Add(new ReductionField<ReductionFieldValue>
                            {
                                Id = Field.Id,
                                FieldName = Field.FieldName,
                                DisplayName = Field.FieldDisplayName,
                                ValueDelimiter = Field.FieldDelimiter,
                                StructureType = Field.StructureType,
                                Values = DbContext.HierarchyFieldValue
                                    .Where(fv => fv.HierarchyFieldId == Field.Id)
                                    .OrderBy(hfv => hfv.Value)
                                    .Select(fv => new ReductionFieldValue { Value = fv.Value })
                                    .ToList(),
                            });
                            break;

                        case ContentTypeEnum.Html:
                        case ContentTypeEnum.Pdf:
                        case ContentTypeEnum.FileDownload:
                        default:
                            // Should never get here because no hierarchy fields should exist for non-reducible types
                            break;
                    }
                }

                return ReturnObject;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static ContentReductionHierarchy<ReductionFieldValueSelection> Apply(
            ContentReductionHierarchy<ReductionFieldValue> hierarchy,
            ContentReductionHierarchy<ReductionFieldValueSelection> selections
        )
        {
            var result = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = hierarchy?.RootContentItemId ?? selections.RootContentItemId,
            };

            foreach (var field in hierarchy?.Fields ?? new List<ReductionField<ReductionFieldValue>> { })
            {
                var sourceField = selections.Fields.SingleOrDefault((f) => f.FieldName == field.FieldName);
                var values = field.Values.Select((v) => new ReductionFieldValueSelection
                {
                    Id = v.Id,
                    Value = v.Value,
                    SelectionStatus = sourceField?.Values
                        .SingleOrDefault((sv) => sv.Value == v.Value)?.SelectionStatus ?? false,
                }).ToList();
                result.Fields.Add(new ReductionField<ReductionFieldValueSelection>
                {
                    Id = field.Id,
                    FieldName = field.FieldName,
                    DisplayName = field.DisplayName,
                    StructureType = field.StructureType,
                    ValueDelimiter = field.ValueDelimiter,
                    Values = values,
                });
            }

            return result;
        }
    }
}

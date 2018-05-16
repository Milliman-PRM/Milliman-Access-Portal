/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A model representing full hierarchy information used to perform content reduction. 
 * DEVELOPER NOTES: 
 */

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using MapDbContextLib.Context;

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

        public long RootContentItemId { get; set; }

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

        /// <summary>Build hierarchy of selections for a selection group</summary>
        /// <remarks>This builds a nested data structure to represent a hierarchy from several flat tables.</remarks>
        /// <param name="SelectionGroupId">The selection group whose selections are to be gathered</param>
        /// <param name="Selections">Any changes to the current selections to effect in the returned hierarchy</param>
        /// <returns>ContentReductionHierarchy</returns>
        public static ContentReductionHierarchy<ReductionFieldValueSelection> GetFieldSelectionsForSelectionGroup(ApplicationDbContext DbContext, long SelectionGroupId, long[] Selections = null)
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
            var SelectedHierarchyFieldValues = (Selections == null)
                ? SelectionGroup.SelectedHierarchyFieldValueList
                : Selections;

            ContentReductionHierarchy<ReductionFieldValueSelection> ContentReductionHierarchy = new ContentReductionHierarchy<ReductionFieldValueSelection>
            {
                RootContentItemId = SelectionGroup.RootContentItemId,
            };

            var RelatedHierarchyFields = DbContext.HierarchyField
                .Where(hf => hf.RootContentItemId == SelectionGroup.RootContentItemId);
            foreach (var HierarchyField in RelatedHierarchyFields)
            {
                var HierarchyFieldValues = DbContext.HierarchyFieldValue
                    .Where(hfv => hfv.HierarchyFieldId == HierarchyField.Id)
                    .Select(hfv => new ReductionFieldValueSelection
                    {
                        Id = hfv.Id,
                        Value = hfv.Value,
                        SelectionStatus = SelectedHierarchyFieldValues.Contains(hfv.Id),
                    })
                    .ToArray();

                // TODO: Create ReductionFieldBase and extend for each content type
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
                    default:
                        ReductionField = null;
                        break;
                }

                ContentReductionHierarchy.Fields.Add(ReductionField);
            }

            return ContentReductionHierarchy;
        }
    }
}

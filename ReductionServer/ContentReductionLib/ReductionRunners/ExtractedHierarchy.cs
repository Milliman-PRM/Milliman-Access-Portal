/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Internal representation of the reduction hierarchy found in the master content for a job, independent of any types in task queue source applications
 * DEVELOPER NOTES: If different queue sources are ever added these classes should add corresponding cast operators to convert from this type to 
 * the type that is meaningful to the application.  This is then used in the class that interfaces with the task queue (e.g. class MapDbJobMonitor).
 */

using System.Linq;
using System.Collections.Generic;
using MapDbContextLib.Models;

namespace ContentReductionLib.ReductionRunners
{
    /// <summary>
    /// Needs to be all public because json serializer requires full access
    /// </summary>
    public class ExtractedField
    {
        public string FieldName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Delimiter { get; set; } = string.Empty;
        public string ValueStructure { get; set; } = string.Empty;
        public List<string> FieldValues { get; set; } = new List<string>();

        /// <summary>
        /// Explicit cast operator for conversion to MapDbContextLib.Models.ReductionField
        /// </summary>
        /// <param name="ExtractedField"></param>
        public static explicit operator ReductionField<ReductionFieldValue>(ExtractedField ExtractedField)
        {
            ReductionField<ReductionFieldValue> NewField = new ReductionField<ReductionFieldValue>
            {
                FieldName = ExtractedField.FieldName,
                DisplayName = ExtractedField.DisplayName,
                ValueDelimiter = ExtractedField.Delimiter,
                Values = ExtractedField.FieldValues
                                       .Select(v => new ReductionFieldValue { Value = v })
                                       .ToArray()
            };

            switch (ExtractedField.ValueStructure.ToLower())
            {
                case "list":
                    NewField.StructureType = FieldStructureType.Flat;
                    break;
                case "tree":
                    NewField.StructureType = FieldStructureType.Tree;
                    break;
                default:
                    NewField.StructureType = FieldStructureType.Unknown;
                    break;
            }

            return NewField;
        }
    }

    /// <summary>
    /// Needs to be all public because json serializer requires full access
    /// </summary>
    public class ExtractedHierarchy
    {
        public List<ExtractedField> Fields = new List<ExtractedField>();

        /// <summary>
        /// Explicit cast operator for conversion to MapDbContextLib.Models.ContentReductionHierarchy
        /// </summary>
        public static explicit operator ContentReductionHierarchy<ReductionFieldValue>(ExtractedHierarchy Hierarchy)
        {
            ContentReductionHierarchy<ReductionFieldValue> ReturnObject = new ContentReductionHierarchy<ReductionFieldValue>();
            foreach (ExtractedField Field in Hierarchy.Fields)
            {
                ReturnObject.Fields.Add((ReductionField<ReductionFieldValue>)Field);
            };

            return ReturnObject;
        }
    }
}

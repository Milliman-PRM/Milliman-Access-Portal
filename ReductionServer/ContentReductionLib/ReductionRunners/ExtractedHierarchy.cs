/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Internal representation of the reduction hierarchy found in the master content for a job, independent of any types in task queue source applications
 * DEVELOPER NOTES: If different queue sources are ever added these classes should add corresponding cast operators to convert from this type to 
 * the type that is meaningful to the application.  This is then used in the class that interfaces with the task queue (e.g. class MapDbJobMonitor).
 */

using System;
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

        public static explicit operator ReductionField(ExtractedField ExtractedField)
        {
            ReductionField NewField = new ReductionField
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

        public static explicit operator ContentReductionHierarchy(ExtractedHierarchy Hierarchy)
        {
            ContentReductionHierarchy ReturnObject = new ContentReductionHierarchy();
            foreach (ExtractedField Field in Hierarchy.Fields)
            {
                ReturnObject.Fields.Add((ReductionField)Field);
            };

            return ReturnObject;
        }
    }
}

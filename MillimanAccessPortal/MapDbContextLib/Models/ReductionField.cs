/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A base class representing a content hierarchy field
 * DEVELOPER NOTES: 
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib;
using MapDbContextLib.Context;

namespace MapDbContextLib.Models
{
    public class ReductionField
    {
        public ReductionField() { }

        /// <summary>
        /// Constructor intended to assist deserializing from database to this model class with or without selections
        /// </summary>
        /// <param name="Field"></param>
        /// <param name="ValuesArg"></param>
        /// <param name="SelectedValues"></param>
        public ReductionField(HierarchyField Field, IEnumerable<HierarchyFieldValue> ValuesArg, IEnumerable<string> SelectedValues = null)
        {
            FieldName = Field.FieldName;
            DisplayName = Field.FieldDisplayName;
            StructureType = Field.StructureType;
            ValueDelimiter = Field.FieldDelimiter;
            // TODO: Consider using foreach instead of for
            for (int Counter=0; Counter< ValuesArg.Count(); Counter++)
            {
                string Val = ValuesArg.ElementAt(Counter).Value;
                if (SelectedValues != null)  // apply the provided selection list
                {
                    this.Values = this.Values.Append(new ReductionFieldValueSelection { Value = Val, SelectionStatus = SelectedValues.Contains(Val) }).ToArray();
                }
                else
                {
                    this.Values = this.Values.Append(new ReductionFieldValue { Value = Val }).ToArray();
                }
            }
        }

        public long Id { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public FieldStructureType StructureType { get; set; } = FieldStructureType.Unknown;
        public string ValueDelimiter { get; set; } = null;

        /// <summary>
        /// Instance of this could also be type child class ReductionFieldValueSelection
        /// </summary>
        public ReductionFieldValue[] Values { get; set; } = new ReductionFieldValue[0];
    }
}


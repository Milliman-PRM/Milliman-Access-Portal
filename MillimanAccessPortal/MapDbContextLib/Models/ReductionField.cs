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
    public class ReductionField<T> where T : ReductionFieldValue
    {
        public long Id { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public FieldStructureType StructureType { get; set; } = FieldStructureType.Unknown;
        public string ValueDelimiter { get; set; } = null;

        /// <summary>
        /// Instance of this could also be type child class ReductionFieldValueSelection
        /// </summary>
        public T[] Values { get; set; } = new T[0];
    }
}


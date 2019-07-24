/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: Originally intended for use to specify a list of file extension strings associated with a content type enumeration
 */

using System.Collections.Generic;
using System.Linq;

namespace System.ComponentModel.DataAnnotations
{
    //
    // Summary:
    //     Provides a general-purpose attribute that lets you specify a list of strings
    //     for types and members of entity partial classes.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class StringListAttribute : Attribute
    {
        public string[] StringArray { get; set; }

        public List<string> GetStringList()
        {
            return StringArray.ToList();
        }
    }
}

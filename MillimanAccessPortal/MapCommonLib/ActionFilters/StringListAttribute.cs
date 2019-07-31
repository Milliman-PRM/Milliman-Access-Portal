/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: Originally intended for use to specify a list of file extension strings associated with a content type enumeration
 */

using System.Collections.Generic;
using System.Linq;

namespace System.ComponentModel.DataAnnotations
{
    public enum StringListKey
    {
        Unspecified = 0,
        FileExtensions = 1,
    }

    //
    // Summary:
    //     Provides a general-purpose attribute that lets you specify a list of strings
    //     for types and members of entity partial classes.
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class StringListAttribute : Attribute
    {
        public StringListKey Key { get; set; } = StringListKey.Unspecified;
        public string[] StringArray { get; set; }

        public List<string> GetStringList()
        {
            return StringArray.ToList();
        }
    }
}

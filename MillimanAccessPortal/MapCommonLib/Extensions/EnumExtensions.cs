/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Generic extensions applicable to enum types
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace System
{
    public static class EnumExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="enumVal"></param>
        /// <returns></returns>
        public static string GetDisplayValueString<TEnum>(this TEnum enumVal) where TEnum : Enum
        {
            DisplayAttribute att = enumVal.GetAttribute<DisplayAttribute>();
            return att?.Name ?? enumVal.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="enumVal"></param>
        /// <returns></returns>
        public static List<string> GetStringList<TEnum>(this TEnum enumVal) where TEnum : Enum
        {
            StringListAttribute att = enumVal.GetAttribute<StringListAttribute>();
            return att?.GetStringList();
        }

        /// <summary>
        /// Retrieves an `Attribute` declared on the specified value of the specified enumeration type
        /// </summary>
        /// <typeparam name="TAtt">The type of the attribute to be found and returned</typeparam>
        /// <param name="enumVal">The enumeration value for which the attribute should be returned</param>
        /// <returns>Returns null if no matching attribute is found</returns>
        private static TAtt GetAttribute<TAtt>(this Enum enumVal) 
            where TAtt : Attribute 
            => enumVal.GetType()
                      .GetMember(enumVal.ToString())
                      .First()
                      .GetCustomAttribute<TAtt>();
    }
}

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
        /// Returns a Name string value assigned to an individual enum value through the use of <c>[DisplayAttribute(Name=...)]</c>
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="enumVal"></param>
        /// <param name="defaultIfNotDefined">Set true to return <c>default(string)</c> if no Name has been assigned</param>
        /// <returns>The assigned string, otherwise by default the actual name of this enumeration value</returns>
        public static string GetDisplayNameString<TEnum>(this TEnum enumVal, bool defaultIfNotDefined = false) where TEnum : Enum
        {
            DisplayAttribute att = enumVal.GetAttribute<DisplayAttribute>();
            return att?.Name ?? (defaultIfNotDefined ? default : enumVal.ToString());
        }

        /// <summary>
        /// Returns a Description string value assigned to an individual enum value through the use of <c>[DisplayAttribute(Description=...)]</c>
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="enumVal"></param>
        /// <param name="defaultIfNotDefined">Set true to return <c>default(string)</c> if no Description has been assigned</param>
        /// <returns>The assigned string, otherwise by default the actual name of this enumeration value</returns>
        public static string GetDisplayDescriptionString<TEnum>(this TEnum enumVal, bool defaultIfNotDefined = false) where TEnum : Enum
        {
            DisplayAttribute att = enumVal.GetAttribute<DisplayAttribute>();
            return att?.Description ?? (defaultIfNotDefined ? default : enumVal.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="enumVal"></param>
        /// <returns></returns>
        public static List<string> GetStringList<TEnum>(this TEnum enumVal, StringListKey key) where TEnum : Enum
        {
            IEnumerable<StringListAttribute> atts = enumVal.GetAllAttributes<StringListAttribute>();
            return atts.SingleOrDefault(a => a.Key == key)?.GetStringList();
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

        /// <summary>
        /// Retrieves all`Attribute`s declared on the specified value of the specified enumeration type
        /// </summary>
        /// <typeparam name="TAtt">The type of the attribute to be found and returned</typeparam>
        /// <param name="enumVal">The enumeration value for which the attribute should be returned</param>
        /// <returns>Returns null if no matching attribute is found</returns>
        private static IEnumerable<TAtt> GetAllAttributes<TAtt>(this Enum enumVal)
            where TAtt : Attribute
            => enumVal.GetType()
                      .GetMember(enumVal.ToString())
                      .First()
                      .GetCustomAttributes<TAtt>();
    }
}

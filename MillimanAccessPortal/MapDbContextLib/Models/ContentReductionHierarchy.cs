/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A model representing full hierarchy information used to perform content reduction. 
 * DEVELOPER NOTES: 
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

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
            string str = ContentReductionHierarchy<ReductionFieldValue>.SerializeJson(ObjFromJson);
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

        public static string SerializeJson(ContentReductionHierarchy<T> hierarchy)
        {
            return JsonConvert.SerializeObject(hierarchy, Formatting.Indented);
        }
    }
}

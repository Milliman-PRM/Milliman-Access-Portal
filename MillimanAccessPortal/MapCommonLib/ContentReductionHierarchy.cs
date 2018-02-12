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

namespace MapCommonLib
{
    public class ContentReductionHierarchy
    {
        public ContentReductionHierarchy()
        {}

        public static void Test(string JsonString = "")
        {
            if (JsonString == string.Empty)
            {
                JsonString = File.ReadAllText(@"C:\Users\Tom.Puckett\Desktop\testHierarchy.json");
            }
            ContentReductionHierarchy ObjFromJson = Deserialize(JsonString);
            string str = SerializeJson(ObjFromJson);
        }

        public ReductionFieldBase[] Fields { get; set; } = new ReductionFieldBase[0];
        public long RootContentItemId { get; set; }

        public static ContentReductionHierarchy Deserialize(string JsonString)
        {
            try  // Can fail e.g. if structureType field value does not match an enumeration value name
            {
                return JsonConvert.DeserializeObject<ContentReductionHierarchy>(JsonString);
            }
            catch
            {
                return null;
            }
        }

        public static string SerializeJson(ContentReductionHierarchy Arg)
        {
            return JsonConvert.SerializeObject(Arg, Formatting.Indented);
        }
    }
}

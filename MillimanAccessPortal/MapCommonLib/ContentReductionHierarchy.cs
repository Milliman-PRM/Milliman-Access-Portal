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

        public void Test(string JsonString)
        {
            var JsonObj = Deserialize(JsonString);
            var str = SerializeJson(JsonObj);
        }

        public ReductionFieldNodeBase RootNode { get; set; }
        public long RootContentItemId { get; set; }

        public static ContentReductionHierarchy Deserialize(string JsonString)
        {
            return JsonConvert.DeserializeObject<ContentReductionHierarchy>(JsonString);
        }

        public static string SerializeJson(ContentReductionHierarchy Arg)
        {
            return JsonConvert.SerializeObject(Arg, Formatting.Indented);
        }
    }
}

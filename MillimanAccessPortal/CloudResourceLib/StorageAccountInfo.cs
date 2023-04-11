using System.Collections.Generic;

namespace CloudResourceLib
{
    public class StorageAccountInfo
    {
        public StorageAccountInfo(string name, List<string> keys)
        {
            Name=name;
            Keys=keys;
        }

        public string Name { get; init; }
        public List<string> Keys { get; init; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/file-shares/create?tabs=HTTP#shareaccesstier
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ShareAccessTierEnum
    {
        Cool,
        Hot,
        Premium,
        TransactionOptimized,
    }
}

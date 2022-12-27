using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FileStorageLib.AzureRestApiModels
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum KeySourceEnum
    {
        [EnumMember(Value = "Microsoft.Keyvault")]
        MicrosoftKeyvault,

        [EnumMember(Value = "Microsoft.Storage")]
        MicrosoftStorage,
    }
}

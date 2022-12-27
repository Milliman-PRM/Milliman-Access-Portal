using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileStorageLib.AzureRestApiModels
{
    // https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts/create?tabs=HTTP#allowedcopyscope 
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AllowedCopyScopeEnum
    {
        AAD,
        PrivateLink,
    }
}

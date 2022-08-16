using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapCommonLib
{
    public class MicrosoftAuthenticationResponse
    {
        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { set; get; }

        [JsonProperty(PropertyName = "scope")]
        public string Scope { set; get; }

        [JsonProperty(PropertyName = "expires_in")]
        public int ExpiresIn { set; get; }

        [JsonProperty(PropertyName = "ext_expires_in")]
        public int ExtExpiresIn { set; get; }

        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { set; get; }
    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A view model class to initialize a razor view with a message and buttons
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.SharedModels
{
    public class MessageWithButtons
    {
        public string Message { get; set; }
        public List<ConfiguredButton> Buttons { get; set; }
    }

    public class ConfiguredButton
    {
        public string Value { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public Dictionary<string,string> RouteData { get; set; } = new Dictionary<string, string>();
        public string Method { get; set; } = "post";
        public string ButtonClass { get; set; } = "blue-button";
    }
}

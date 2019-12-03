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
    public class MessageModel
    {
        public List<string> MessagesAboveButtons { get; set; } = new List<string>();
        public List<ConfiguredButton> Buttons { get; set; } = new List<ConfiguredButton>();
        public List<string> MessagesBelowButtons { get; set; } = new List<string>();
    }

    public class ConfiguredButton
    {
        public string Value { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Dictionary<string,string> RouteData { get; set; } = new Dictionary<string, string>();
        public string Method { get; set; } = "post";
        public string ButtonClass { get; set; } = "link-button";
    }
}

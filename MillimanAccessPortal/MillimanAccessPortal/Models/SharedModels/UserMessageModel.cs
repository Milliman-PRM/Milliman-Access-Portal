/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A view model class to initialize a razor view with a message and buttons
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MillimanAccessPortal.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.SharedModels
{
    public class UserMessageModel
    {
        /// <summary>
        /// Constructor with optional initializer of message string(s)
        /// </summary>
        /// <param name="Messages">String(s) that will initialize the MessagesAboveButtons property of this instance.  Caller can pass separate arguments (or array)</param>
        public UserMessageModel(params string[] Messages)
        {
            PrimaryMessages.AddRange(Messages);
        }

        public List<string> PrimaryMessages { get; set; } = new List<string>();

        /// <summary>
        /// Default is one OK button linking to the action AuthorizedContentController.Index
        /// </summary>
        public List<ConfiguredButton> Buttons { get; set; } = new List<ConfiguredButton>
            { new ConfiguredButton { Value = "OK", Action = nameof(AuthorizedContentController.Index), Controller = nameof(AuthorizedContentController).Replace("Controller", "") } };

        public List<string> SecondaryMessages { get; set; } = new List<string>();
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

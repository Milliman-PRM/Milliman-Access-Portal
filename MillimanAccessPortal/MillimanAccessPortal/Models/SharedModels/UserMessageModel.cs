/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A view model class to initialize a razor view with a message and buttons
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MillimanAccessPortal.Controllers;
using MapCommonLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.SharedModels
{
    public enum UserMessageEnum
    {
        [Display(Description = "This account has been locked out, please try again later.")]
        AccountLocked = 1,

        [Display(Description = "Your MAP account is disabled due to inactivity.  Please contact your Milliman consultant, or email {MapSupportEmail}.")]
        AccountDisabled,

        [Display(Description = "Your MAP account is currently suspended.  If you believe that this is an error, please contact your Milliman consultant, or email {MapSupportEmail}.")]
        AccountSuspended,

        [Display(Description = "Your MAP account has not been activated. Please look for a welcome email from {MapSupportEmail} and follow instructions in that message to activate the account.")]
        AccountNotActivated,

        [Display(Description = "Login failed, please try again later.")]
        AccountNotAllowed,

        [Display(Description = "Your organization's authenticating domain did not return your user name. Please email {MapSupportEmail} with this error message.")]
        SsoUserNameNotReturned,

        [Display(Description = "Your login does not have a MAP account.  Please contact your Milliman consultant, or email {MapSupportEmail}.")]
        SsoNoMapAccount,
    }

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

        public UserMessageModel(UserMessageEnum messageEnum)
        {
            string message = messageEnum.GetDisplayDescriptionString();
            message = message.Replace("{MapSupportEmail}", $"<a href =\"mailto:{GlobalFunctions.MillimanSupportEmailAlias}\">{GlobalFunctions.MillimanSupportEmailAlias}</a>");

            PrimaryMessages.Add(message);
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

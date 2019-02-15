/*
 * CODE OWNERS: Ben Wyatt, Tom Puckett
 * OBJECTIVE: Provide a controller for AJAX calls to send mail, leveraging MessageServices
 * DEVELOPER NOTES: 
 */

using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MillimanAccessPortal.DataQueries;
using MillimanAccessPortal.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Controllers
{
    public class MessageController : Controller
    {
        IMessageQueue _messageQueue { get; set; }
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly StandardQueries Queries;
        
        public MessageController(
            IConfiguration configuration,
            IMessageQueue messageQueueArg,
            UserManager<ApplicationUser> UserManagerArg,
            StandardQueries QueriesArg)
        {
            _configuration = configuration;
            _messageQueue = messageQueueArg;
            UserManager = UserManagerArg;
            Queries = QueriesArg;
        }

        /// <summary>
        /// Send a message to one or more recipients, optionally overriding the default sender from smtp.json
        /// </summary>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="senderAddress"></param>
        /// <param name="senderName"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendEmail (IEnumerable<string> recipients, string subject, string message, string senderAddress=null, string senderName=null)
        {
            Log.Verbose($"Entered MessageController.SendEmail action for recipients <{string.Join(", ", recipients)}>, subject <{subject}>");
            _messageQueue.QueueEmail(recipients, subject, message, senderAddress, senderName);

            Log.Verbose($"In MessageController.SendEmail action: email queued successfully");
            return Ok();
        }

        /// <summary>
        /// Send a message to a single recipient, from the default sender defined in smtp.json
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendEmail (string recipient, string subject, string message)
        {
            Log.Verbose($"Entered MessageController.SendEmail action for recipient <recipient>, subject <{subject}>");
            _messageQueue.QueueEmail(recipient, subject, message);

            Log.Verbose($"In MessageController.SendEmail action: email queued successfully");
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendSupportEmail(string subject, string message)
        {
            Log.Verbose($"Entered MessageController.SendSupportEmail action for subject <{subject}>");

            var user = await Queries.GetCurrentApplicationUser(User);
            var senderAddress = user.Email;
            var senderName = $"{user.FirstName} {user.LastName}";
            var recipient = _configuration.GetValue<string>("SupportEmailAddress");

            _messageQueue.QueueEmail(new List<string> { recipient }, subject, message, senderAddress, senderName);

            Log.Verbose($"In MessageController.SendSupportEmail action: email queued successfully");
            return Ok();
        }

        /// <summary>
        /// Send a message to the specified recipient from the current user
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmailFromUser ([FromForm] IFormCollection collection)
        {
            Log.Verbose($"Entered MessageController.SendEmailFromUser action with keys <{string.Join(",", collection.Keys)}>");

            if (!collection.Keys.Contains("subject") ||
                !collection.Keys.Contains("message") ||
                !collection.Keys.Contains("recipient"))
            {
                return BadRequest("Form data does not contain required key");
            }

            string subject = collection["subject"].ToString();
            string message = collection["message"].ToString();
            string recipient = collection["recipient"].ToString();

            Console.WriteLine("Sending mail to " + recipient);
            // Get the current user's name and email address
            var user = await Queries.GetCurrentApplicationUser(User);
            string senderAddress = user.Email;
            string senderName = $"{user.FirstName} {user.LastName}";

            _messageQueue.QueueEmail(new List<string> { recipient }, subject, message, senderAddress, senderName);

            Log.Verbose($"In MessageController.SendEmailFromUser action: email queued successfully");
            return Ok();
        }
    }
}
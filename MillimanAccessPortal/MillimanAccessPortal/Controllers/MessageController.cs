/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Provide a controller for AJAX calls to send mail, leveraging MessageServices
 * DEVELOPER NOTES: 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EmailQueue;
using Microsoft.Extensions.Logging;
using MillimanAccessPortal.Services;
using MillimanAccessPortal.DataQueries;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Controllers
{
    public class MessageController : Controller
    {
        IMessageQueue _mailSender { get; set; }
        private readonly UserManager<ApplicationUser> UserManager;
        private readonly StandardQueries Queries;
        
        public MessageController(
            IMessageQueue mailSenderArg,
            UserManager<ApplicationUser> UserManagerArg,
            StandardQueries QueriesArg)
        {
            _mailSender = mailSenderArg;
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

            bool Result =_mailSender.QueueEmail(recipients, subject, message, senderAddress, senderName);

            if (Result)
            {
                return Ok();
            }
            else
            {
                // Send attempt failed. Log failure and return failure code.
                return BadRequest();
            }

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
            bool Result = _mailSender.QueueEmail(recipient, subject, message);

            if (Result)
            {
                return Ok();
            }
            else
            {
                // Send attempt failed. Log failure and return failure code.
                return BadRequest();
            }
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
            string senderName = user.UserName;

            bool Result = _mailSender.QueueEmail(new List<string> { recipient }, subject, message, senderAddress, senderName);

            if (Result)
            {
                return Ok();
            }
            else
            {
                // Send attempt failed. Log failure and return failure code.
                return BadRequest();
            }
        }
    }
}
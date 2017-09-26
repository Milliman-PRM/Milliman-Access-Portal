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

namespace MillimanAccessPortal.Controllers
{
    public class MessageController : Controller
    {
        MessageServices _mailSender { get; set; }
        
        public MessageController(MessageServices mailSenderArg)
        {
            _mailSender = mailSenderArg;
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
        public IActionResult SendMail (IEnumerable<string> recipients, string subject, string message, string senderAddress=null, string senderName=null)
        {

            Task sendResult = _mailSender.SendEmailAsync(recipients, subject, message, senderAddress, senderName);

            if (sendResult.IsCompleted)
            {
                return Ok();
            }
            else
            {
                // Send attempt failed. Log failure and return failure code.
                return StatusCode(400);
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
        public IActionResult SendMail (string recipient, string subject, string message)
        {
            Task sendResult = _mailSender.SendEmailAsync(recipient, subject, message);

            if (sendResult.IsCompleted)
            {
                return Ok();
            }
            else
            {
                // Send attempt failed. Log failure and return failure code.
                return StatusCode(400);
            }
        }
    }
}
/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: API for sending messages to users
 * DEVELOPER NOTES: This class is an expansion of the one included with the Visual Studio template used to create MAP
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using EmailQueue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MillimanAccessPortal.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link https://go.microsoft.com/fwlink/?LinkID=532713
    public class MessageQueueServices : IMessageQueue
    {
        private ILogger _logger { get; }
        private MailSender _sender { get; set; }
        private readonly IConfiguration ApplicationConfig;

        /// <summary>
        /// Constructor. Consumes ILoggerFactory from application.
        /// </summary>
        /// <param name="loggerFactory"></param>
        public MessageQueueServices(
            ILoggerFactory loggerFactory,
            IConfiguration AppConfigurationArg)
        {
            _logger = loggerFactory.CreateLogger<MessageQueueServices>();
            ApplicationConfig = AppConfigurationArg;

            _sender = new MailSender(_logger);
        }

        /// <summary>
        /// A pass through to the same method with signature accepting a collection of message recipients
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="senderAddress">Optional</param>
        /// <param name="senderName">Optional</param>
        /// <returns></returns>
        public bool QueueEmail(string recipient, string subject, string message, string senderAddress = null, string senderName = null, bool addGlobalDisclaimer = true)
        {
            return QueueEmail(new string[] { recipient }, subject, message, senderAddress, senderName, addGlobalDisclaimer);
        }

        /// <summary>
        /// Queues an email message for sending
        /// </summary>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="senderAddress">Optional</param>
        /// <param name="senderName">Optional</param>
        /// <returns></returns>
        public bool QueueEmail(IEnumerable<string> recipients, string subject, string message, string senderAddress = null, string senderName = null, bool addGlobalDisclaimer = true)
        {
            if (addGlobalDisclaimer)
            {
                string disclaimer = ApplicationConfig.GetValue<string>("Global:EmailDisclaimer");
                if (!string.IsNullOrWhiteSpace(disclaimer))
                {
                    message += $"{Environment.NewLine}{Environment.NewLine}{disclaimer}";
                }
            }

            return _sender.QueueMessage(recipients, subject, message, senderAddress, senderName);
        }

        public bool QueueSms(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return false;
        }
    }
}

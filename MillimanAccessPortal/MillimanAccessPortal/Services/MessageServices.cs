/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: API for sending messages to users
 * DEVELOPER NOTES: This class is an expansion of the one included with the Visual Studio template used to create MAP
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using EmailQueue;
using Microsoft.Extensions.Logging;

namespace MillimanAccessPortal.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link https://go.microsoft.com/fwlink/?LinkID=532713
    public class MessageServices : IEmailSender, ISmsSender
    {
        private SmtpConfig _smtpConfig { get; }
        private ILogger _logger { get; }
        private MailSender _sender { get; set; }

        /// <summary>
        /// Constructor. Consumes injected SMTP configuration from application.
        /// </summary>
        /// <param name="smtpConfigArg"></param>
        public MessageServices(IOptions<SmtpConfig> smtpConfigArg, ILoggerFactory loggerFactory)
        {
            _smtpConfig = smtpConfigArg.Value;
            _logger = loggerFactory.CreateLogger<MessageServices>();
            _sender = new MailSender(_logger);
        }

        /// <summary>
        /// A more in-depth message sender, which allows more configuration than the default (below)
        /// </summary>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="senderAddress"></param>
        /// <param name="senderName"></param>
        /// <returns></returns>
        public Task SendMailAsync(List<string> recipients, string subject, string message, string senderAddress = null, string senderName = null)
        {
            if (String.IsNullOrEmpty(senderAddress))
            {
                senderAddress = _smtpConfig.SmtpFromAddress;
            }
            if (String.IsNullOrEmpty(senderName))
            {
                senderName = _smtpConfig.SmtpFromName;
            }

            Task queueResult = _sender.QueueMessage(new MailItem(
                    subject,
                    message,
                    recipients,
                    senderAddress,
                    senderName
                    )
                );

            return Task.FromResult(queueResult);
        }

        /// <summary>
        /// This is the default message call, which is preserved primarily to retain compatibility with modules expecting it to exist.
        /// 
        /// This also provides a simple way to send a message to a single recipient from the system default sender.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task SendEmailAsync(string email, string subject, string message)
        {

            string senderAddress = _smtpConfig.SmtpFromAddress;
            string senderName = _smtpConfig.SmtpFromName;
            List<string> recipient = new List<string>
            {
                email
            };

            Task queueResult = SendMailAsync(recipient, subject, message, senderAddress, senderName);
            return Task.FromResult(queueResult);
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }
}

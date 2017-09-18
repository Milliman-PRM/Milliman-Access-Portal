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

        /// <summary>
        /// Constructor. Consumes injected SMTP configuration from application.
        /// </summary>
        /// <param name="smtpConfigArg"></param>
        public MessageServices(IOptions<SmtpConfig> smtpConfigArg, ILoggerFactory loggerFactory)
        {
            _smtpConfig = smtpConfigArg.Value;
            _logger = loggerFactory.CreateLogger<MessageServices>();
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                // Configure message
                var MailMessage = new MimeMessage();
                MailMessage.From.Add(new MailboxAddress(_smtpConfig.SmtpFromName, _smtpConfig.SmtpFromAddress));
                MailMessage.To.Add(new MailboxAddress(email));
                MailMessage.Subject = subject;
                MailMessage.Body = new TextPart("plain")
                {
                    Text = message
                };

                // Send mail
                using (var client = new SmtpClient())
                {
                    client.Connect(_smtpConfig.SmtpServer, _smtpConfig.SmtpPort, MailKit.Security.SecureSocketOptions.Auto);
                    client.Send(MailMessage);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(2, ex, "Failed to send mail");
            }
            
            return Task.FromResult(0);
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }
}

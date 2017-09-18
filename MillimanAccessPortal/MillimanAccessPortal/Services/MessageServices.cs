using System;
using System.Collections.Generic;
using System.Linq;
using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link https://go.microsoft.com/fwlink/?LinkID=532713
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            // TODO: Add support for email templates (future update?)

            try
            {
                // Configure message
                
                // TODO: Get configuration from json
                string FromAddress = "prm.support@milliman.com";
                string FromName = "Milliman PRM Analytics Support";
                string SmtpServer = "smtp.milliman.com";
                int SmtpPortNumber = 25;

                var MailMessage = new MimeMessage();
                MailMessage.From.Add(new MailboxAddress(FromName, FromAddress)));
                MailMessage.To.Add(new MailboxAddress(email));
                MailMessage.Subject = subject;
                MailMessage.Body = new TextPart("plain")
                {
                    Text = message
                };

                // Send mail
                using (var client = new SmtpClient())
                {
                    client.Connect(SmtpServer, SmtpPortNumber, false);
                    client.Send(MailMessage);
                    client.Disconnect(true);
                }

            }
            catch (Exception ex)
            {
                // TODO: Add exception handling
            }

            // Plug in your email service here to send an email.
            return Task.FromResult(0);
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }
}

/*
 * CODE OWNERS: Ben Wyatt, Tom Puckett
 * OBJECTIVE:Queueing system for outgoing email
 * DEVELOPER NOTES: 
 */

using Serilog;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MailKit.Net.Smtp;

namespace EmailQueue
{
    public class MailSender
    {
        /// <summary>
        /// Initiates asynchronous send of an email
        /// </summary>
        /// <param name="Cfg"></param>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="senderAddress"></param>
        /// <param name="senderName"></param>
        public static async Task<bool> SendEmailAsync(SmtpConfig Cfg, IEnumerable<string> recipients, string subject, string message, string senderAddress, string senderName)
        {
            return await Task.Run(() => SendEmail(Cfg, recipients, subject, message, senderAddress, senderName));
        }

        /// <summary>
        /// Sends the requested email
        /// </summary>
        /// <param name="Cfg"></param>
        /// <param name="recipients"></param>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        /// <param name="senderAddress"></param>
        /// <param name="senderName"></param>
        /// <returns></returns>
        private static bool SendEmail(SmtpConfig Cfg, IEnumerable<string> recipients, string subject, string message, string senderAddress, string senderName)
        {
            if (String.IsNullOrEmpty(senderAddress))
            {
                senderAddress = Cfg.SmtpFromAddress;
            }
            if (String.IsNullOrEmpty(senderName))
            {
                senderName = Cfg.SmtpFromName;
            }

            MailItem mailItem = new MailItem(subject, message, recipients, senderAddress, senderName);

            do
            {
                mailItem.sendAttempts++;

                try
                {
                    // Send mail
                    using (var client = new SmtpClient())
                    {
                        if (!string.IsNullOrWhiteSpace(Cfg.SmtpUsername) && !string.IsNullOrWhiteSpace(Cfg.SmtpPassword))
                        {
                            client.Connect(Cfg.SmtpServer, Cfg.SmtpPort, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
                            client.Authenticate(Cfg.SmtpUsername, Cfg.SmtpPassword);
                        }
                        else
                        {
                            client.Connect(Cfg.SmtpServer, Cfg.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                        }

                        client.Send(mailItem.message);
                        client.Disconnect(true);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to send email on attempt #{mailItem.sendAttempts}");
                }
            }
            while (mailItem.sendAttempts < Cfg.MaximumSendAttempts);

            return false;
        }
    }
}

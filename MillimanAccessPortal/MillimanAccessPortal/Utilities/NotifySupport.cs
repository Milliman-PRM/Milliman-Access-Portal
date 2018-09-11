/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Utility class to send notifications of issues to MAP Support
 * DEVELOPER NOTES: Avoid using this except for issues which might warrant immediate attention
 */
using Microsoft.Extensions.Configuration;
using MillimanAccessPortal.Services;

namespace MillimanAccessPortal.Utilities
{
    public class NotifySupport
    {
        private readonly IMessageQueue _messageSender;
        private readonly IConfiguration _configuration;

        public NotifySupport(IMessageQueue queueArg, IConfiguration configArg)
        {
            _messageSender = queueArg;
            _configuration = configArg;
        }

        /// <summary>
        /// Send a message to the configured support email address
        /// </summary>
        /// <param name="messageArg"></param>
        /// <returns></returns>
        public bool sendSupportMail(string messageArg, string reason)
        {
            string supportAddress = _configuration["SupportEmailAddress"] ?? "map.support@milliman.com";
            string sender = _configuration["SmtpFromAddress"] ?? "map.support@milliman.com";
            return _messageSender.QueueEmail(supportAddress, $"Automated support notification - {reason}", messageArg, sender);
        }
    }
}

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
            string supportEmail = _configuration.GetValue("SupportEmailAddress", "map.support@milliman.com");
            string sender = _configuration.GetValue("SmtpFromAddress", "map.support@milliman.com");
            return _messageSender.QueueEmail(supportEmail, $"Automated support notification - {reason}", messageArg, sender);
        }

        /// <summary>
        /// Send a messaged to the configured Infrastructure & Security team address
        /// </summary>
        /// <param name="messageArg"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool sendSecurityMail(string messageArg, string reason)
        {
            string securityEmailAddress = _configuration.GetValue("SecurityEmailAlias", "prm.security@milliman.com");
            string sender = _configuration.GetValue("SmtpFromAddress", "prm.security@milliman.com");
            return _messageSender.QueueEmail(securityEmailAddress, $"Automated support notification - {reason}", messageArg, sender);
        }
    }
}

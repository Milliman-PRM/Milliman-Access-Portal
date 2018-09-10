/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: Utility class to send notifications of issues to MAP Support
 * DEVELOPER NOTES: Avoid using this except for issues which might warrant immediate attention
 */
using Microsoft.Extensions.Configuration;
using MillimanAccessPortal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public bool sendSupportMail(string messageArg)
        {
            return _messageSender.QueueEmail(supportAddress(), "Automated support notification", messageArg, supportSender());
        }

        private string supportAddress()
        {
            return _configuration["SupportEmailAddress"] ?? "map.support@milliman.com";
        }

        private string supportSender()
        {
            return _configuration["SmtpFromAddress"] ?? "map.support@milliman.com";
        }
    }
}

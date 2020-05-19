/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE: API for sending messages to users
 * DEVELOPER NOTES: This class is an expansion of the one included with the Visual Studio template used to create MAP
 */

using System.Collections.Generic;
using Prm.EmailQueue;

namespace MillimanAccessPortal.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link https://go.microsoft.com/fwlink/?LinkID=532713
    public class MessageQueueServices : IMessageQueue
    {
        private MailSender _sender { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public MessageQueueServices()
        {
            _sender = new MailSender();
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
        public bool QueueEmail(string recipient, string subject, string message, string senderAddress = null, string senderName = null)
        {
            return QueueEmail(new string[] { recipient }, subject, message, senderAddress, senderName);
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
        public bool QueueEmail(IEnumerable<string> recipients, string subject, string message, string senderAddress = null, string senderName = null)
        {
            return _sender.QueueMessage(recipients, subject, message, senderAddress, senderName);
        }

        public bool QueueSms(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return false;
        }
    }
}

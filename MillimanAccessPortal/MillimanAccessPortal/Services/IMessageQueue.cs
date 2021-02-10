using System.Collections.Generic;

namespace MillimanAccessPortal.Services
{
    public interface IMessageQueue
    {
        bool QueueMessage(IEnumerable<string> recipients, IEnumerable<string> cc, IEnumerable<string> bcc, string subject, string message, string senderAddress, string senderName, string disclaimer = null);
        bool QueueEmail(IEnumerable<string> recipients, string subject, string message, string senderAddress = null, string senderName = null);
        bool QueueEmail(string recipient, string subject, string message, string senderAddress = null, string senderName = null);
        bool QueueSms(string number, string message);
    }
}
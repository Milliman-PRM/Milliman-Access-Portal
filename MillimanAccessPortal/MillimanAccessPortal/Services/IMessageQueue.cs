using System.Collections.Generic;

namespace MillimanAccessPortal.Services
{
    public interface IMessageQueue
    {
        bool QueueEmail(IEnumerable<string> recipients, string subject, string message, string senderAddress = null, string senderName = null);
        bool QueueEmail(string recipient, string subject, string message, string senderAddress = null, string senderName = null);
        bool QueueSms(string number, string message);
    }
}
using System.Collections.Generic;

namespace MillimanAccessPortal.Services
{
    public interface IMessageQueue
    {
        void QueueEmail(IEnumerable<string> recipients, string subject, string message, string senderAddress = null, string senderName = null);
        void QueueEmail(string recipient, string subject, string message, string senderAddress = null, string senderName = null);
        void QueueSms(string number, string message);
    }
}
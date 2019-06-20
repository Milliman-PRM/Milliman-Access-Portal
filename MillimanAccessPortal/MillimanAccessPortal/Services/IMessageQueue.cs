using System.Collections.Generic;

namespace MillimanAccessPortal.Services
{
    public interface IMessageQueue
    {
        bool QueueEmail(IEnumerable<string> recipients, string subject, string message, string senderAddress = null, string senderName = null, bool addGlobalDisclaimer = true);
        bool QueueEmail(string recipient, string subject, string message, string senderAddress = null, string senderName = null, bool addGlobalDisclaimer = true);
        bool QueueSms(string number, string message);
    }
}
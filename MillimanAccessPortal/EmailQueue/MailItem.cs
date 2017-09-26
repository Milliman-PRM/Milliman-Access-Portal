using System;
using System.Collections.Generic;
using System.Text;
using MimeKit;

namespace EmailQueue
{
    public class MailItem
    {
        public MimeMessage message { get; }
        public int sendAttempts { get; set; } = 0;

        public MailItem(string subject, string messageBody, IEnumerable<string> recipients, string senderAddress, string senderName)
        {
            // Configure required fields for message
            
            MimeEntity encodedBody = new TextPart("plain")
            {
                Text = messageBody
            };

            MailboxAddress sender = new MailboxAddress(senderName, senderAddress);
            List<InternetAddress> senderList = new List<InternetAddress>();
            senderList.Add(sender);

            List<InternetAddress> recipientList = new List<InternetAddress>();
            foreach (string recipient in recipients)
            {
                recipientList.Add(new MailboxAddress(recipient));
            }

            message = new MimeMessage(senderList, recipientList, subject, encodedBody);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using MimeKit;

namespace EmailQueue
{
    class MailItem
    {
        public MimeMessage message { get; }
        public int sendAttempts { get; set; } = 0;

        public MailItem(string subject, string messageBody, string recipientAddress)
        {
            message = new MimeMessage();
            message.To.Add(new MailboxAddress(recipientAddress));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = messageBody
            };
        }
    }
}

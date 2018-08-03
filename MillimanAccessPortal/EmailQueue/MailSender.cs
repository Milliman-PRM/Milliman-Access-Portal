/*
 * CODE OWNERS: Ben Wyatt
 * OBJECTIVE:Queueing system for outgoing email
 * DEVELOPER NOTES: 
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;

namespace EmailQueue
{
    public class MailSender
    {
        private static ConcurrentQueue<MailItem> Messages = new ConcurrentQueue<MailItem>();
        private static Task WorkerTask = null;
        private static int InstanceCount = 0;
        private static object ThreadSafetyLock = new object();
        public static SmtpConfig smtpConfig = null;
        private static ILogger _logger { get; set; }

        public static void ConfigureMailSender(SmtpConfig config)
        {
            lock (ThreadSafetyLock)
            {
                smtpConfig = config;
            }
        }

        public MailSender(ILogger loggerArg)
        {
            if (smtpConfig == null)
            {
                throw new Exception("Attempt to instantiate MailSender before initializing!");
            }

            InstanceCount++;
            _logger = loggerArg;
            
            if (WorkerTask == null || (WorkerTask.Status != TaskStatus.Running && WorkerTask.Status != TaskStatus.WaitingToRun))
            {
                WorkerTask = Task.Run(() => ProcessQueueEvents());
                while (WorkerTask.Status != TaskStatus.Running)
                {
                    Thread.Sleep(1);
                }
            }
        }

        public bool QueueMessage(IEnumerable<string> recipients, string subject, string message, string senderAddress, string senderName)
        {
            try
            {
                if (String.IsNullOrEmpty(senderAddress))
                {
                    senderAddress = smtpConfig.SmtpFromAddress;
                }
                if (String.IsNullOrEmpty(senderName))
                {
                    senderName = smtpConfig.SmtpFromName;
                }

                MailItem mailItem = new MailItem( subject, message, recipients, senderAddress, senderName);

                Messages.Enqueue(mailItem);
            }
            catch
            {
                return false;
            }

            return true;
        }

        ~MailSender()
        {
            lock (ThreadSafetyLock)
            {
                InstanceCount--;
                if (InstanceCount == 0 && WaitForWorkerThreadEnd(1000))  // Not the best stategy
                {
                    WorkerTask = null;
                }
            }
        }

        /// <summary>
        /// Waits for the worker thread to end (if running)
        /// </summary>
        /// <param name="MaxWaitMs">Time limit to wait (in ms)</param>
        /// <returns>true if thread is not running at time of return</returns>
        public bool WaitForWorkerThreadEnd(int MaxWaitMs = 0)
        {
            if (WorkerTask != null && WorkerTask.Status == TaskStatus.Running)
            {
                WorkerTask = Task.Run(() => ProcessQueueEvents());
                return WorkerTask.Wait(MaxWaitMs);
            }
            return true;
        }

        /// <summary>
        /// Process and send messages in the queue
        /// </summary>
        /// <param name="Arg"></param>
        private static void ProcessQueueEvents()
        {
            while (InstanceCount > 0)
            {
                if (Messages.Count > 0)
                {
                    MailItem nextMessage;
                    while (Messages.TryDequeue(out nextMessage))
                    {
                        SendEmail(nextMessage);
                    }
                }
                Thread.Sleep(20);
            }
        }

        /// <summary>
        /// Send mail
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static bool SendEmail(MailItem message)
        {
            message.sendAttempts++;

            try
            {
                // Send mail
                using (var client = new SmtpClient())
                {
                    client.Connect(smtpConfig.SmtpServer, smtpConfig.SmtpPort, MailKit.Security.SecureSocketOptions.None);

                    // Attempt to authenticate if credentials are configured
                    // Will throw an exception if authentication fails
                    if (!string.IsNullOrEmpty(smtpConfig.SmtpUsername) && !string.IsNullOrEmpty(smtpConfig.SmtpPassword))
                    {
                        client.Authenticate(smtpConfig.SmtpUsername, smtpConfig.SmtpPassword);
                    }

                    client.Send(message.message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {

                if (message.sendAttempts > smtpConfig.MaximumSendAttempts)
                {
                    // TODO what is the semantics of 2 here?
                    _logger.LogError(2, ex, $"Failed to send email on attempt #{message.sendAttempts}, limit exceed, message discarded.");
                    return false;
                }

                _logger.LogWarning(2, ex, $"Failed to send email on attempt #{message.sendAttempts}");
                Messages.Enqueue(message);

                // allow return true; while max retry limit is not exceeded ?
            }

            return true;
        }
    }
}

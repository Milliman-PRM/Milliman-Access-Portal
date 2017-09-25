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
        private static SmtpConfig smtpConfig = null;
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
        }

        public static Task QueueMessage(MailItem mailItem)
        {
            Messages.Enqueue(mailItem);

            return Task.FromResult(0);
        }

        ~MailSender()
        {
            lock (ThreadSafetyLock)
            {
                InstanceCount--;
                if (InstanceCount == 0 && WaitForWorkerThreadEnd(1000))  // Not the best stategy
                {
                    WorkerTask = null;
                    smtpConfig = null;
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
                        SendEmailAsync(nextMessage).Wait();
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
        private static Task SendEmailAsync(MailItem message)
        {
            try
            {
                // Send mail
                using (var client = new SmtpClient())
                {
                    client.Connect(smtpConfig.SmtpServer, smtpConfig.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                    client.Send(message.message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // If sending fails, we need to re-queue the message with an increased attempt count.
                // TODO: Add configurable maximum number of attempts to SmtpConfig
                message.sendAttempts++;
                Messages.Enqueue(message);

                _logger.LogWarning(2, ex, "Failed to send mail");
            }

            return Task.FromResult(0);
        }
    }
}

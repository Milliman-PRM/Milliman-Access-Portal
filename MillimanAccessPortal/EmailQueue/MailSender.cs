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
using MailKit;
using MimeKit;
using MillimanAccessPortal.Services;
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
        private ILogger _logger { get; }

        public static void ConfigureMailSender(SmtpConfig config)
        {
            lock (ThreadSafetyLock)
            {
                smtpConfig = config;
            }
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
                return WorkerTask.Wait(MaxWaitMs);
            }
            return true;
        }

        public Task SendEmailAsync(MimeMessage message)
        {
            try
            {
                // Send mail
                using (var client = new SmtpClient())
                {
                    client.Connect(smtpConfig.SmtpServer, smtpConfig.SmtpPort, MailKit.Security.SecureSocketOptions.None);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(2, ex, "Failed to send mail");
            }

            return Task.FromResult(0);
        }
    }
}

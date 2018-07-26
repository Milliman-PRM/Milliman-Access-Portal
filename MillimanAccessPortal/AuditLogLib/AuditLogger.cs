using AuditLogLib.Event;
using AuditLogLib.Services;
using MapCommonLib;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AuditLogLib
{
    public class AuditLogger : IAuditLogger
    {
        // TODO instead of an in-process queue, switch to use an out of process asynchronous message queue.
        // The issue here is that if the process is terminated or crashes, any unprocessed log messages in the queue could be lost.  
        private static ConcurrentQueue<AuditEvent> LogEventQueue = new ConcurrentQueue<AuditEvent>();
        private static Task WorkerTask = null;
        private static int InstanceCount = 0;
        private static object ThreadSafetyLock = new object();
        public static AuditLoggerConfiguration Config
        {
            set;
            private get;
        }

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly string _assemblyName = Assembly.GetEntryAssembly().FullName;

        public AuditLogger()
        {
            InstantiateLogger();
        }

        public AuditLogger(
            IHttpContextAccessor context = null,
            UserManager<ApplicationUser> userManager = null)
        {
            _contextAccessor = context;
            _userManager = userManager;

            InstantiateLogger();
        }

        ~AuditLogger()
        {
            // Watch out how this lock is used.  The worker thread needs to be able to stop itself when InstanceCount == 0
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
        /// Perform on construction
        /// </summary>
        private void InstantiateLogger()
        {
            lock (ThreadSafetyLock)
            {
                if (Config == null)
                {
                    string msg = "Attempt to instantiate AuditLogger before initializing!";
                    GlobalFunctions.TraceWriteLine($"{msg}{Environment.NewLine}{new StackTrace().ToString()}");
                    throw new ApplicationException(msg);
                }

                InstanceCount++;
                if (WorkerTask == null || (WorkerTask.Status != TaskStatus.Running && WorkerTask.Status != TaskStatus.WaitingToRun))
                {
                    WorkerTask = Task.Run(() => ProcessQueueEvents(Config));
                    while (WorkerTask.Status != TaskStatus.Running)
                    {
                        Thread.Sleep(1);
                    }
                }
            }
        }

        /// <summary>
        /// Simplest logging method, does not conform to ILogger, requires a fully formed event object
        /// </summary>
        /// <param name="Event">Event data to be logged. Use AuditEvent.New method to enforce proper creation</param>
        public async virtual void Log(AuditEvent Event)
        {
            var context = _contextAccessor?.HttpContext;
            var user = await _userManager?.GetUserAsync(context?.User);

            Event.SessionId = context?.Session?.Id;
            Event.User = user?.ToString();
            Event.Assembly = _assemblyName;

            LogEventQueue.Enqueue(Event);
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

        /// <summary>
        /// Worker thread main entry point
        /// </summary>
        /// <param name="Arg">Configuration object</param>
        private static void ProcessQueueEvents(object Arg)
        {
            AuditLoggerConfiguration Config = (AuditLoggerConfiguration)Arg;

            while (InstanceCount > 0)
            {
                if (LogEventQueue.Count > 0)
                {
                    using (AuditLogDbContext Db = AuditLogDbContext.Instance(Config.AuditLogConnectionString))
                    {
                        List<AuditEvent> NewEventsToStore = new List<AuditEvent>();

                        while (LogEventQueue.TryDequeue(out AuditEvent NextEvent))
                        {
                            NewEventsToStore.Add(NextEvent);
                        }

                        Db.AuditEvent.AddRange(NewEventsToStore);
                        Db.SaveChanges();
                    }
                }

                Thread.Sleep(20);
            }
        }
    }
}

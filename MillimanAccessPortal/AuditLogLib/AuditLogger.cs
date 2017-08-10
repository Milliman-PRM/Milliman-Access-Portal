using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace AuditLogLib
{
    public class AuditLogger : ILogger
    {
        private static int InstanceCount = 0;
        private static ConcurrentQueue<AuditEvent> LogEventQueue = new ConcurrentQueue<AuditEvent>();
        private static Task WorkerTask = null;
        private static object ThreadSafetyLock = new object();
        private static bool NoStopSignal = true;
        private AuditLoggerConfiguration Config = null;

        public AuditLogger(AuditLoggerConfiguration ConfigArg = null)
        {
            if (ConfigArg == null)
            {
                Config = new AuditLoggerConfiguration();
            }

            lock (ThreadSafetyLock)
            {
                InstanceCount++;

                if (WorkerTask == null || WorkerTask.Status != TaskStatus.Running)
                {
                    NoStopSignal = true;
                    WorkerTask = Task.Run(() => ProcessQueueEvents());
                }
            }
        }

        ~AuditLogger()
        {
            lock(ThreadSafetyLock)
            {
                InstanceCount--;
                if (InstanceCount == 0)
                {
                    // Signal the worker thread to gracefully stop asap.
                    NoStopSignal = false;
                    // WorkerTask.Wait(); // ? maybe check WorkerTask.TaskStatus, but it may be unnecessary.  Don't block the calling thread. 
                }
            }
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception = null, Func<TState, Exception, string> formatter = null)
        {
            if (eventId.Id < AuditEventId.AuditEventBaseId || eventId.Id > AuditEventId.AuditEventMaxId)
                return;

            AuditEvent NewEvent = new AuditEvent
            {
                EventDetailObject = state,
                EventType = eventId.Name,
                TimeStamp = DateTime.Now,
                User = "Tom",  // TODO Get the UserId from somewhere
            };

            LogEventQueue.Enqueue(NewEvent);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Don't use this until it gets implemented
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            // TODO not sure what is expected here.  Implement and adjust the method comment. 
            return null;
        }

        /// <summary>
        /// Worker thread main entry point
        /// </summary>
        /// <param name="Arg"></param>
        private static void ProcessQueueEvents(object Arg = null)
        {
            while (NoStopSignal)
            {
                if (LogEventQueue.Count > 0)
                {
                    using (AuditLogDbContext Db = AuditLogDbContext.Instance)
                    {
                        List<AuditEvent> NewEventsToStore = new List<AuditEvent>();

                        AuditEvent NextEvent;
                        while (LogEventQueue.TryDequeue(out NextEvent))
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

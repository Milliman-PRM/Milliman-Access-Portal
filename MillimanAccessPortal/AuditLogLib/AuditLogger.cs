using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace AuditLogLib
{
    public class AuditLogger : ILogger
    {
        private static int InstanceCount = 0;
        private static Queue<AuditEvent> LogEventQueue = new Queue<AuditEvent>();
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
                    NoStopSignal = false;
                    // maybe check WorkerTask.TaskStatus, but it may be unnecessary and could block the calling thread. 
                }
            }
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception = null, Func<TState, Exception, string> formatter = null)
        {
            if (state.GetType() != typeof(AuditLogLib.AuditEvent))
            {
                //throw new Exception("AuditLogger.Log called with unexpected state object type");
            }
            AuditEvent NewEvent = new AuditEvent
            {
                EventDetailObject = state as string,
                EventType = "some type",
                TimeStamp = DateTime.Now,
                User = "Tom",
            };

            lock (ThreadSafetyLock)
            {
                LogEventQueue.Enqueue(NewEvent);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            // TODO not sure what is expected here
            return null;
        }

        private static void ProcessQueueEvents(object Arg = null)
        {
            while (NoStopSignal)
            {
                if (LogEventQueue.Count > 0)
                {
                    using (AuditLogDbContext Db = AuditLogDbContext.Instance)
                    {
                        List<AuditEvent> NewEventsToStore = new List<AuditEvent>();
                        lock (ThreadSafetyLock)
                        {
                            while (LogEventQueue.Count > 0)
                            {
                                NewEventsToStore.Add(LogEventQueue.Dequeue());
                            }
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

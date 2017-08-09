using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace AuditLogLib
{
    public class AuditLogger : ILogger
    {
        private static int InstanceCount = 0;
        private static Queue<AuditEvent> LogEventQueue = new Queue<AuditEvent>();
        private static Task WorkerTask = null;
        private static object ThreadSafetyLock = new object();

        public AuditLogger()
        {
            lock(ThreadSafetyLock)
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
                    // TODO terminate the task
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

            LogEventQueue.Enqueue(NewEvent);
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
            while (true /*while task is not signalled to cancel*/)
            {
                using (var db = AuditLogDbContext.Instance)
                {
                    while (LogEventQueue.Count > 0)
                    {
                        AuditEvent E = LogEventQueue.Dequeue();

                        // TODO persist the event to db
                    }
                }


            }
        }
    }
}

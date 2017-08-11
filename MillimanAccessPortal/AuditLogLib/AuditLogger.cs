using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using System.Reflection;

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
            Config = ConfigArg != null ? ConfigArg : new AuditLoggerConfiguration(); 

            lock (ThreadSafetyLock)
            {
                InstanceCount++;

                if (WorkerTask == null || WorkerTask.Status != TaskStatus.Running)
                {
                    NoStopSignal = true;
                    WorkerTask = Task.Run(() => ProcessQueueEvents(Config));
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

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="logLevel">Provide one of the enum values</param>
        /// <param name="eventId">Intended to receive one of the static properties of class AuditEventId</param>
        /// <param name="state">Object of any type, but should follow conventions</param>
        /// <param name="exception">Use null if no exception is being documented</param>
        /// <param name="formatter">If provided, should be compatible with state argument</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception = null, Func<TState, Exception, string> formatter = null)
        {
            if (eventId.Id < AuditEventId.AuditEventBaseId || eventId.Id > AuditEventId.AuditEventMaxId)
                return;

            AuditEvent NewEvent = null;

            if (state.GetType() == typeof(AuditEvent))
            {
                NewEvent = state as AuditEvent;
                NewEvent.EventType = eventId.Name;
            }
            else
            {
                NewEvent = new AuditEvent
                {
                    EventType = eventId.Name,
                    TimeStamp = DateTime.Now,
                };

                // TODO This code is experimental and not done.  I am fishing for the property names in the anonymous object "state".  
                Type t = state.GetType();
                var p = t.GetTypeInfo();
                foreach (var Property in p.DeclaredProperties)
                {
                    switch (Property.Name)
                    {
                        case "UserName":
                            NewEvent.User = Property.GetValue(state) as string;
                            break;
                        case "Source":
                            NewEvent.Source = Property.GetValue(state) as string;
                            break;
                        case "Detail":
                            NewEvent.EventDetailObject = Property.GetValue(state);
                            break;
                        case "Summary":
                            NewEvent.Summary = Property.GetValue(state) as string;
                            break;
                        default:
                            break;
                    }
                }
            }

            // Some day instead of an in-process queue, switch to use of MSMQ and a system service to do persistence that the worker thread does now.
            // The issue here is that if the process is terminated or crashes, any unprocessed log messages in the queue could be lost.  
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
            AuditLoggerConfiguration Config = Arg as AuditLoggerConfiguration;

            while (NoStopSignal)
            {
                if (LogEventQueue.Count > 0)
                {
                    using (AuditLogDbContext Db = AuditLogDbContext.Instance(Config.ConnectionString))
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

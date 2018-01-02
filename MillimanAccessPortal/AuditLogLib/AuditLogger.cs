using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Reflection;
using AuditLogLib.Services;

namespace AuditLogLib
{
    public class AuditLogger : IAuditLogger
    {
        // TODO instead of an in-process queue, switch to use an out of process asynchronous message queue.
        // Hint, MSMQ was an idea but that probably will never be supported in .NET Core since it is a Windows only service.  
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

        public AuditLogger()
        {
            lock (ThreadSafetyLock)
            {
                if (Config == null)
                {
                    throw new Exception("Attempt to instantiate AuditLogger before initializing!");
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
        /// Simplest logging method, does not conform to ILogger, requires a fully formed event object
        /// </summary>
        /// <param name="Event">Event data to be logged. Use AuditEvent.New method to enforce proper creation</param>
        public virtual void Log(AuditEvent Event)
        {
            LogEventQueue.Enqueue(Event);
        }

        /// <summary>
        /// Compliant with ILogger interface, which is not the primary intended type of use for this class. 
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="LogLevel">Provide one of the enum values</param>
        /// <param name="EventId">Intended to receive one of the static properties of class AuditEventId</param>
        /// <param name="ParamObject">Object of any type, but should follow conventions</param>
        /// <param name="exception">Use null if no exception is being documented</param>
        /// <param name="formatter">If provided, should be compatible with state argument</param>
        public void Log<ParamObj>(LogLevel LogLevel, AuditEventId EventId, ParamObj ParamObject, Exception exception = null, Func<ParamObj, Exception, string> formatter = null)
        {
            if (EventId.Id < AuditEventId.AuditEventBaseId || EventId.Id > AuditEventId.AuditEventMaxId)
                return;

            AuditEvent NewEvent = null;

            if (ParamObject.GetType() == typeof(AuditEvent))
            {
                NewEvent = ParamObject as AuditEvent;
                NewEvent.EventType = EventId.Name;
            }
            else
            {
                NewEvent = new AuditEvent
                {
                    EventType = EventId.Name,
                    TimeStamp = DateTime.Now,
                };

                Type t = ParamObject.GetType();
                var p = t.GetTypeInfo();
                foreach (var Property in p.DeclaredProperties)
                {
                    switch (Property.Name)
                    {
                        case "UserName":
                            NewEvent.User = Property.GetValue(ParamObject) as string;
                            break;
                        case "Source":
                            NewEvent.Source = Property.GetValue(ParamObject) as string;
                            break;
                        case "Detail":
                            NewEvent.EventDetailObject = Property.GetValue(ParamObject);
                            break;
                        case "Summary":
                            NewEvent.Summary = Property.GetValue(ParamObject) as string;
                            break;
                        default:
                            break;
                    }
                }
            }

            LogEventQueue.Enqueue(NewEvent);
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

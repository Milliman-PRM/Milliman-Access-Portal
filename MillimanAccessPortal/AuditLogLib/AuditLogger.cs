using AuditLogLib.Event;
using AuditLogLib.Models;
using AuditLogLib.Services;
using MapCommonLib;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MapDbContextLib.Context;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AuditLogLib
{
    public class AuditLogger : IAuditLogger
    {
        private static ConcurrentQueue<AuditEvent> LogEventQueue = new ConcurrentQueue<AuditEvent>();
        private static Task WorkerTask = null;
        private static int InstanceCount = 0;
        private static object ThreadSafetyLock = new object();
        private static int RetryCount = 0;
        public static AuditLoggerConfiguration Config
        {
            set;
            private get;
        }

        private readonly IHttpContextAccessor _contextAccessor = null;
        private readonly string _assemblyName = Assembly.GetEntryAssembly().GetName().Name;

        public AuditLogger()
        {
            InstantiateLogger();
        }

        public AuditLogger(IHttpContextAccessor context = null)
        {
            _contextAccessor = context;

            InstantiateLogger();
        }

        ~AuditLogger()
        {
            // Watch out how this lock is used.  The worker thread needs to be able to stop itself when InstanceCount == 0
            lock (ThreadSafetyLock)
            {
                InstanceCount--;
                if (InstanceCount == 0 && WaitForWorkerThreadEnd(3000))  // Not the best stategy
                {
                    WorkerTask = null;
                    RetryCount = 0;
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
                    throw new ApplicationException(msg);
                }

                InstanceCount++;
                if (WorkerTask == null || !new[] { TaskStatus.Running, TaskStatus.WaitingToRun }.Contains(WorkerTask.Status))
                {
                    WorkerTask = Task.Factory.StartNew(() => ProcessQueueEvents(Config));
                }
            }
        }

        public virtual void Log(AuditEvent Event)
        {
            Log(Event, null, null);
        }

        public virtual void Log(AuditEvent Event, string UserNameArg)
        {
            Log(Event, UserNameArg, null);
        }

        /// <summary>
        /// Simplest logging method, does not conform to ILogger, requires a fully formed event object
        /// </summary>
        /// <param name="Event">Event data to be logged. Use AuditEvent.New method to enforce proper creation</param>
        /// <param name="UserNameArg">Caller provided user name, if provided, will be used</param>
        /// <param name="SessionIdArg">Caller provided session ID, if provided, will be used</param>
        public virtual void Log(AuditEvent Event, string UserNameArg, string SessionIdArg)
        {
            if (_contextAccessor == null)
            {
                Event.User = UserNameArg;
                Event.SessionId = SessionIdArg;
            }
            else
            {
                try
                {
                    Event.User = UserNameArg ?? _contextAccessor.HttpContext?.User?.Identity?.Name;
                    Event.SessionId = SessionIdArg ?? _contextAccessor.HttpContext?.Session?.Id;
                }
                catch (Exception e) // Nothing should stop this from proceding
                {
                    Serilog.Log.Error(e, "In AuditLogger.Log(), exception while accessing _contextAccessor.HttpContext?.User?.Identity?.Name or _contextAccessor.HttpContext?.Session?.Id");
                }
            }

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
            if (WorkerTask != null && new[] { TaskStatus.Running, TaskStatus.WaitingToRun }.Contains(WorkerTask.Status))
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

                        try
                        {
                            while (LogEventQueue.TryDequeue(out AuditEvent NextEvent))
                            {
                                NewEventsToStore.Add(NextEvent);
                            }

                            Db.AuditEvent.AddRange(NewEventsToStore);
                            Db.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            Serilog.Log.Error(e, "In AuditLogger.ProcessQueueEvents(), exception while accessing the ConcurrentQueue or saving events to database");

                            if (RetryCount < 5)
                            {
                                // Re-queue the messages to be logged
                                foreach (AuditEvent RecoveredEvent in NewEventsToStore)
                                {
                                    LogEventQueue.Enqueue(RecoveredEvent);
                                }
                                RetryCount++;
                                Thread.Sleep(2000);
                            }
                            else
                            {
                                RetryCount = 0;
                            }
                        }
                    }
                }

                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Query the AuditEvent table of the context based on query expressions(s) provided by the caller
        /// </summary>
        /// <param name="serverFilters">Filter expressions to be translated to SQL and submitted to postgreSQL</param>
        /// <param name="mapDb"></param>
        /// <param name="orderIsDescending"></param>
        /// <param name="clientFilters">Optional - Filter expressions to be applied to the result of the database query</param>
        /// <param name="limit">Optional - Limits the number of responses using the EF .take() method</param>
        /// <returns></returns>
        public async Task<List<ActivityEventModel>> GetAuditEventsAsync(List<Expression<Func<AuditEvent, bool>>> serverFilters, ApplicationDbContext mapDb, bool orderIsDescending, List<Expression<Func<AuditEvent, bool>>> clientFilters = null, int limit = -1)
        {
            List<AuditEvent> filteredAuditEvents = default;
            using (AuditLogDbContext auditDb = AuditLogDbContext.Instance(Config.AuditLogConnectionString))
            {
                IQueryable<AuditEvent> serverQuery = auditDb.AuditEvent;
                foreach (Expression<Func<AuditEvent, bool>> whereClause in serverFilters)
                {
                    serverQuery = serverQuery.Where(whereClause);
                }

                serverQuery = orderIsDescending
                    ? serverQuery.OrderByDescending(e => e.TimeStampUtc)
                    : serverQuery.OrderBy(e => e.TimeStampUtc);

                if (limit > -1)
                {
                    serverQuery = serverQuery.Take(limit);
                }

                filteredAuditEvents = await serverQuery.ToListAsync();
            }

            if (clientFilters != null)
            {
                IQueryable<AuditEvent> clientQuery = filteredAuditEvents.AsQueryable();
                foreach (Expression<Func<AuditEvent, bool>> whereClause in clientFilters)
                {
                    clientQuery = clientQuery.Where(whereClause);
                }

                filteredAuditEvents = clientQuery.ToList();
            }

            // Find the first/last names for all event usernames in the event list
            IEnumerable<string> allUserNames = filteredAuditEvents.Select(e => e.User).Distinct();
            IDictionary<string, ActivityEventModel.Names> eventNamesDict = mapDb != null && await mapDb.Database.CanConnectAsync()
                                                                         ? await mapDb.ApplicationUser
                                                                                      .Where(u => allUserNames.Contains(u.UserName))
                                                                                      .Select(u => new ActivityEventModel.Names { UserName = u.UserName, LastName = u.LastName, FirstName = u.FirstName })
                                                                                      .ToDictionaryAsync(u => u.UserName)
                                                                         :new Dictionary<string, ActivityEventModel.Names>();

            return filteredAuditEvents.Select(e => ActivityEventModel.Generate(e, !string.IsNullOrEmpty(e.User) && eventNamesDict.ContainsKey(e.User)
                                                                                  ? eventNamesDict[e.User] 
                                                                                  : ActivityEventModel.Names.Empty))
                                      .ToList();
        }
    }
}

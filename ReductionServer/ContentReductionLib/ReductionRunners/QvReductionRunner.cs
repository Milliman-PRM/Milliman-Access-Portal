/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: The reduction runner class for Qlikview content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using AuditLogLib;
using QmsApi;

namespace ContentReductionLib.ReductionRunners
{
    internal class QvReductionRunner : ReductionRunnerBase
    {
        // TODO Get these from configuration
        const string QmsUrl = "http://indy-qvtest01:4799/QMS/Service";
        AuditLogger Logger = new AuditLogger();  // Can't instantiate unless static AuditLogger.Config is initialized (should be done once in ProcessManager)

        internal QvReductionRunner()
        {
            TestAuditLogging();
            TestQvConnection();
        }

        /// <summary>
        /// Remove eventually
        /// </summary>
        private void TestAuditLogging()
        {
            AuditEvent e = AuditEvent.New("ContentReductionLib.ReductionRunners.QvReductionRunner()", "Test event", AuditEventId.Unspecified, new { Note = "This is a test", Source = "Reduction server" });
            new AuditLogger().Log(e);
            Thread.Sleep(200);
        }

        /// <summary>
        /// Remove eventually
        /// </summary>
        private void TestQvConnection()
        {
            IQMS Client = QmsClientCreator.New(QmsUrl);

            // test
            ServiceInfo[] Services = Client.GetServicesAsync(ServiceTypes.All).Result;
            ServiceInfo[] Qds = Client.GetServicesAsync(ServiceTypes.QlikViewDistributionService).Result;
        }

        #region Member properties
        internal ContentReductionTask QueueTask
        {
            set;
            private get;
        }

        internal DbContextOptions<ApplicationDbContext> ContextOptions
        {
            set;
            private get;
        }

        #endregion

        internal override bool ValidateInstance()
        {
            return
                QueueTask != null &&
                ContextOptions != null && 
                Logger != null
                ;
        }

        internal override bool ExecuteReduction()
        {
            if (!ValidateInstance())
            {
                return false;
            }

            Guid G = QueueTask.Id;

            PreTaskSetup(G);
            ExtractReductionHierarchy(G);
            CreateReducedContent(G);
            DistributeResults(G);
            Cleanup(G);

            // Update status based on outcome of above steps
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                Db.ContentReductionTask.Find(G).ReductionStatus = ReductionStatusEnum.Reduced;
                Db.SaveChanges();
            }

            return true;
        }

        /// <summary>
        /// Complete this
        /// </summary>
        /// <param name="G"></param>
        private void PreTaskSetup(Guid G)
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {G.ToString()} completed PreTaskSetup");
        }

        /// <summary>
        /// Complete this
        /// </summary>
        /// <param name="G"></param>
        private void ExtractReductionHierarchy(Guid G)
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {G.ToString()} completed ExtractReductionHierarchy");
        }

        /// <summary>
        /// Complete this
        /// </summary>
        /// <param name="G"></param>
        private void CreateReducedContent(Guid G)
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {G.ToString()} completed CreateReducedContent");
        }

        /// <summary>
        /// Complete this
        /// </summary>
        /// <param name="G"></param>
        private void DistributeResults(Guid G)
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {G.ToString()} completed DistributeResults");
        }

        /// <summary>
        /// Complete this
        /// </summary>
        /// <param name="G"></param>
        private void Cleanup(Guid G)
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {G.ToString()} completed Cleanup");
        }

    }
}

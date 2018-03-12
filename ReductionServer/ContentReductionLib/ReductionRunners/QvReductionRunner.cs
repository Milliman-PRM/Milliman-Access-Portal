/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: The reduction runner class for Qlikview content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.IO;
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

            PreTaskSetup();
            ExtractReductionHierarchy();
            CreateReducedContent();
            DistributeResults();
            Cleanup();

            // Update status based on outcome of above steps
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                Db.ContentReductionTask.Find(QueueTask.Id).ReductionStatus = ReductionStatusEnum.Reduced;
                Db.SaveChanges();
            }

            return true;
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private async void PreTaskSetup()
        {
            IQMS Client = QmsClientCreator.New(QmsUrl);

            // Discover the source document folder configured in QDS 
            ServiceInfo[] AllQdsServices = await Client.GetServicesAsync(ServiceTypes.QlikViewDistributionService);
            DocumentFolder[] AllSourceDocFolders = await Client.GetSourceDocumentFoldersAsync(AllQdsServices[0].ID, DocumentFolderScope.All);
            string RootSourceFolder = AllSourceDocFolders[1].General.Path;

            // Create a subfolder to contain initial artifacts of this task
            Directory.CreateDirectory(Path.Combine(RootSourceFolder, QueueTask.Id.ToString()));

            // Deposit initial contents of the task folder
            File.Copy(QueueTask.MasterFilePath, Path.Combine(RootSourceFolder, "Master.qvw"));

            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed PreTaskSetup");
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private void ExtractReductionHierarchy()
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed ExtractReductionHierarchy");
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private void CreateReducedContent()
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed CreateReducedContent");
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private void DistributeResults()
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed DistributeResults");
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private void Cleanup()
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed Cleanup");
        }

    }
}

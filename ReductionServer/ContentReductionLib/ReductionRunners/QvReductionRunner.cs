/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: The reduction runner class for Qlikview content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
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
        AuditLogger Logger = new AuditLogger();  // Exception if static AuditLogger.Config is not initialized (should be done globally for process)

        /// <summary>
        /// Constructor, sets up starting conditions that are associated with the system configuration rather than this specific task.
        /// </summary>
        internal QvReductionRunner()
        {
            // Initialize members
            IQMS Client = QmsClientCreator.New(QmsUrl);

            // Initialize member variables
            QdsServiceInfo = Client.GetServicesAsync(ServiceTypes.QlikViewDistributionService).Result[0];
            SourceDocFolder = Client.GetSourceDocumentFoldersAsync(QdsServiceInfo.ID, DocumentFolderScope.All).Result[1];

            TestAuditLogging();
            //TestQvConnection();
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

        private DocumentFolder SourceDocFolder { get; set; } = null;

        private string WorkingFolderRelative { get; set; } = string.Empty;

        private ServiceInfo QdsServiceInfo { get; set; } = null;

        private string MasterFileName { get { return "Master.qvw"; } }
        #endregion

        internal override bool ValidateInstance()
        {
            bool Result = QueueTask != null &&
                          ContextOptions != null &&
                          Logger != null &&
                          QdsServiceInfo != null &&
                          QdsServiceInfo.ID != Guid.Empty &&
                          SourceDocFolder != null &&
                          Directory.Exists(SourceDocFolder.General.Path);

            if (!Result)
            {
                // TODO log specifically what failed
                Trace.WriteLine("QvReductionRunner.ValidateInstance() failed");
            }

            return Result;
        }

        internal override async Task<bool> ExecuteReduction()
        {
            if (!ValidateInstance())
            {
                using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
                {
                    Db.ContentReductionTask.Find(QueueTask.Id).ReductionStatus = ReductionStatusEnum.Error;
                    Db.SaveChanges();
                }

                return false;
            }

            if (
                // Each of these is responsible to make an appropriate task status update in the case of failure
                ! PreTaskSetup() ||
                ! ExtractReductionHierarchy() ||
                ! CreateReducedContent() ||
                ! DistributeResults() ||
                ! Cleanup()
               )
            {
                return false;
            }

            // Update task with status reduced
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                Db.ContentReductionTask.Find(QueueTask.Id).ReductionStatus = ReductionStatusEnum.Reduced;
                Db.SaveChanges();
            }

            return true;
        }

        /// <summary>
        /// Sets up the starting conditions that are unique to this specific task
        /// </summary>
        private bool PreTaskSetup()
        {
            IQMS Client = QmsClientCreator.New(QmsUrl);

            // Folder is named for the task guid from the database
            WorkingFolderRelative = QueueTask.Id.ToString();

            string WorkingFolderAbsolute = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);
            try
            {
                if (Directory.Exists(WorkingFolderAbsolute))
                {
                    Directory.Delete(WorkingFolderAbsolute, true);
                }
                Directory.CreateDirectory(WorkingFolderAbsolute);
                File.Copy(QueueTask.MasterFilePath, Path.Combine(WorkingFolderAbsolute, MasterFileName));
            }
            catch (System.Exception e)
            {
                Trace.WriteLine($"QvReductionRunner.PreTaskSetup() failed to create task folder {WorkingFolderAbsolute} or copy master file to>{MasterFileName}<, exception message:" + Environment.NewLine + e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private bool ExtractReductionHierarchy()
        {
            // Create ancillary script
            string AncillaryScriptFilePath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, "ancillary_script.txt");
            File.WriteAllText(AncillaryScriptFilePath, "LET DataExtraction=true();");

            // Create task object
            TaskInfo I = CreateHierarchyExtractionTask().Result;

            // run partial reduction

            // Remove ancillary script

            Thread.Sleep(500);
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed ExtractReductionHierarchy");

            return true;
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private bool CreateReducedContent()
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed CreateReducedContent");

            return true;
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private bool DistributeResults()
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed DistributeResults");

            return true;
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private bool Cleanup()
        {
            Thread.Sleep(500);
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed Cleanup");

            return true;
        }

        /// <summary>
        /// Returns Qv DocumentNode object corresponding to the requested file 
        /// </summary>
        /// <param name="RequestedFileName"></param>
        /// <param name="RequestedRelativeFolder">Path relative to the selected source documents folder.</param>
        /// <returns></returns>
        private QmsApi.DocumentNode GetSourceDocumentNode(string RequestedFileName, string RequestedRelativeFolder)
        {
            IQMS QmsClient = QmsClientCreator.New(QmsUrl);

            DocumentNode[] AllDocNodes = QmsClient.GetSourceDocumentNodesAsync(QdsServiceInfo.ID, SourceDocFolder.ID, RequestedRelativeFolder).Result;
            DocumentNode DocNode = AllDocNodes.SingleOrDefault(dn => dn.FolderID == SourceDocFolder.ID
                                                                  && dn.Name == RequestedFileName
                                                                  && dn.RelativePath == RequestedRelativeFolder);

            if (DocNode == null)
            {
                Trace.WriteLine(string.Format($"Did not find SourceDocument '{MasterFileName}' in source documents folder {SourceDocFolder.General.Path}"));
            }
            return DocNode;
        }

        private async Task<QmsApi.TaskInfo> CreateHierarchyExtractionTask()
        {
            QmsApi.TaskInfo ReturnTaskInfo = new TaskInfo();
            string TaskDateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Create new task object
            DocumentTask NewDocumentTask = new DocumentTask();

            NewDocumentTask.ID = Guid.NewGuid();
            NewDocumentTask.QDSID = QdsServiceInfo.ID;
            NewDocumentTask.Document = GetSourceDocumentNode(MasterFileName, WorkingFolderRelative);

            #region General Tab
            NewDocumentTask.Scope |= DocumentTaskScope.General;
            NewDocumentTask.General = new QmsApi.DocumentTask.TaskGeneral();
            NewDocumentTask.General.Enabled = true;
            NewDocumentTask.General.TaskName = $"Hierarchy extraction for task {this.QueueTask.Id.ToString("D")} at {TaskDateTimeStamp}";
            NewDocumentTask.General.TaskDescription = $"Automatically generated by ReductionService at {TaskDateTimeStamp}";
            #endregion

            #region Reload Tab
            NewDocumentTask.Scope |= DocumentTaskScope.Reload;
            NewDocumentTask.Reload = new QmsApi.DocumentTask.TaskReload();
            NewDocumentTask.Reload.Mode = QmsApi.TaskReloadMode.Partial;
            #endregion

            #region Reduce Tab
            NewDocumentTask.Scope |= DocumentTaskScope.Reduce;
            NewDocumentTask.Reduce = new QmsApi.DocumentTask.TaskReduce();
            NewDocumentTask.Reduce.DocumentNameTemplate = string.Empty;
            NewDocumentTask.Reduce.Static = new QmsApi.DocumentTask.TaskReduce.TaskReduceStatic();
            NewDocumentTask.Reduce.Static.Reductions = new QmsApi.TaskReduction[] { new TaskReduction() };
            NewDocumentTask.Reduce.Static.Reductions[0].Type = new QmsApi.TaskReductionType();
            NewDocumentTask.Reduce.Dynamic = new QmsApi.DocumentTask.TaskReduce.TaskReduceDynamic();
            NewDocumentTask.Reduce.Dynamic.Type = new QmsApi.TaskReductionType();
            #endregion

            #region Trigger Tab
            NewDocumentTask.Scope |= QmsApi.DocumentTaskScope.Triggering;
            NewDocumentTask.Triggering = new QmsApi.DocumentTask.TaskTriggering();
            NewDocumentTask.Triggering.ExecutionAttempts = 1;
            NewDocumentTask.Triggering.ExecutionTimeout = 1440;
            NewDocumentTask.Triggering.TaskDependencies = new QmsApi.TaskInfo[] { };
            NewDocumentTask.Triggering.Triggers = new QmsApi.ScheduleTrigger[] {
                    new QmsApi.ScheduleTrigger()
                };
            NewDocumentTask.Triggering.Triggers[0].Enabled = true;
            NewDocumentTask.Triggering.Triggers[0].Type = QmsApi.TaskTriggerType.OnceTrigger;
            ((QmsApi.ScheduleTrigger)NewDocumentTask.Triggering.Triggers[0]).StartAt = DateTime.Now;
            #endregion

            int counter = 0;
            TaskInfo Info;

            IQMS QmsClient = QmsClientCreator.New(QmsUrl);
            QmsClient.SaveDocumentTaskAsync(NewDocumentTask);

            do
            {
                Info = await QmsClient.FindTaskAsync(QdsServiceInfo.ID, QmsApi.TaskType.DocumentTask, NewDocumentTask.General.TaskName);
                counter++;
                System.Threading.Thread.Sleep(500);
            }
            while (Info == null);

            Trace.WriteLine($"Task with ID '{NewDocumentTask.ID.ToString("N")}' successfully saved");

            return ReturnTaskInfo;
        }

    }
}

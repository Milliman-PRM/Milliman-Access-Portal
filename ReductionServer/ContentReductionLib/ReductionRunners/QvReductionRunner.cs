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

        internal override (Guid TaskId, ReductionJobResultEnum Result) ExecuteReduction()
        {
            try
            {
                if (ValidateThisInstance() &&
                    PreTaskSetup() &&
                    ExtractReductionHierarchy().Result &&
                    CreateReducedContent() &&
                    DistributeResults() &&
                    Cleanup()
                   )
                {
                    return (QueueTask.Id, ReductionJobResultEnum.Success);
                }
            }
            catch
            {}

            return (QueueTask.Id, ReductionJobResultEnum.Error);
        }

        internal override bool ValidateThisInstance()
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

        /// <summary>
        /// Sets up the starting conditions that are unique to this specific task
        /// </summary>
        private bool PreTaskSetup()
        {
            IQMS Client = QmsClientCreator.New(QmsUrl);

            WorkingFolderRelative = QueueTask.Id.ToString();  // Folder is named for the task guid from the database
            string WorkingFolderAbsolute = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);
            string MasterFileDestinationPath = Path.Combine(WorkingFolderAbsolute, MasterFileName);

            try
            {
                if (Directory.Exists(WorkingFolderAbsolute))
                {
                    Directory.Delete(WorkingFolderAbsolute, true);
                }
                Directory.CreateDirectory(WorkingFolderAbsolute);
                File.Copy(QueueTask.MasterFilePath, MasterFileDestinationPath);
            }
            catch (System.Exception e)
            {
                Trace.WriteLine($"QvReductionRunner.PreTaskSetup() failed to create folder {WorkingFolderAbsolute} or copy master file {QueueTask.MasterFilePath} to {MasterFileDestinationPath}, exception message:" + Environment.NewLine + e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private async Task<bool> ExtractReductionHierarchy()
        {
            // Create ancillary script
            string AncillaryScriptFilePath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, "ancillary_script.txt");
            File.WriteAllText(AncillaryScriptFilePath, "LET DataExtraction=true();");

            // Create task object
            TaskInfo Info = await CreateHierarchyExtractionQdsTask();

            // run partial reduction
            await RunQdsTask(Info);

            // Remove ancillary script
            // Maybe I don't really need to do this, just remove the task folder at the end of processing? 
            File.Delete(AncillaryScriptFilePath);

            string ReductionSchemeFilePath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, "reduction.scheme.csv");
            foreach (string Line in File.ReadLines(ReductionSchemeFilePath))
            {
                if (Line.Contains("FieldName"))
                {
                    continue;
                }

                string[] Fields = Line.Split(new char[]{','}, StringSplitOptions.None);
                string ContentFieldName = Fields[1];
                string ValuesFileName = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, "fieldvalues." + ContentFieldName + ".csv");
                string[] Values = File.ReadAllLines(ValuesFileName);
                int x = Values.Length;
            }

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

        private async Task<QmsApi.TaskInfo> CreateHierarchyExtractionQdsTask()
        {
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

            IQMS QmsClient = QmsClientCreator.New(QmsUrl);

            await QmsClient.SaveDocumentTaskAsync(NewDocumentTask);
            QmsApi.TaskInfo ReturnTaskInfo = await QmsClient.FindTaskAsync(QdsServiceInfo.ID, QmsApi.TaskType.DocumentTask, NewDocumentTask.General.TaskName);

            Trace.WriteLine($"Task with ID '{NewDocumentTask.ID.ToString("N")}' successfully saved");

            return ReturnTaskInfo;
        }

        private async Task RunQdsTask(QmsApi.TaskInfo TInfo)
        {
            // TODO make these configurable
            TimeSpan MaxStartDelay = new TimeSpan(0, 5, 0);
            TimeSpan MaxElapsedRun = new TimeSpan(0, 5, 0);

            IQMS QmsClient = QmsClientCreator.New(QmsUrl);

            QmsApi.TaskStatus Status;

            // Get task started, this generally requires more than one call to RunTaskAsync
            DateTime StartTime = DateTime.Now;
            do
            {
                if (DateTime.Now - StartTime > MaxStartDelay)
                {
                    Trace.WriteLine($"In QvReductionRunner.RunQdsTask() task {TInfo.ID} failed to start running, aborting");
                    throw new System.Exception($"Qlikview publisher failed to start task {TInfo.ID}");
                }

                await QmsClient.RunTaskAsync(TInfo.ID);
                Thread.Sleep(500);
                Status = await QmsClient.GetTaskStatusAsync(TInfo.ID, TaskStatusScope.Extended);
                Trace.WriteLine($"Starting task waited for {DateTime.Now - StartTime}");
            } while (Status == null || Status.Extended == null || string.IsNullOrEmpty(Status.Extended.StartTime));
            Trace.WriteLine($"In QvReductionRunner.RunQdsTask() task {TInfo.ID} started running after {DateTime.Now - StartTime}");

            // Wait for started task to finish
            DateTime RunningStartTime = DateTime.Now;
            do
            {
                if (DateTime.Now - RunningStartTime > MaxElapsedRun)
                {
                    Trace.WriteLine($"QvReductionRunner.RunQdsTask() elapsed time is {DateTime.Now - RunningStartTime}, aborting");
                    throw new System.Exception($"Qlikview publisher failed to finish task {TInfo.ID}");
                }

                Thread.Sleep(500);
                Status = await QmsClient.GetTaskStatusAsync(TInfo.ID, TaskStatusScope.Extended);
            } while (Status == null || Status.Extended == null || !DateTime.TryParse(Status.Extended.FinishedTime, out DateTime dummy));
            Trace.WriteLine($"In QvReductionRunner.RunQdsTask() task {TInfo.ID} finished running after {DateTime.Now - RunningStartTime}");

            int i = 8;
            /*
            try
            {
                DateTime StartTime = DateTime.Now;

                this.RunTaskSynchronously(TInfo.ID);

                this.AddCleanUpAction(new Action(() => {
                    QmsApi.IQMS QmsClient = ConnectToQMS(this.QVConnectionSettings);

                    Trace.WriteLine(string.Format("Deleting task '{0}'", TInfo.ID.ToString("N")));
                    QmsClient.DeleteTask(TInfo.ID, QMSAPI.TaskType.DocumentTask);
                }));

                DateTime EndTime = DateTime.Now;

                //LogToUser("Hierarchy Task ran on Qlikview Server, duration " + (EndTime-StartTime).ToString("g"));
                Trace.WriteLine("Hierarchy Task ran on Qlikview Server, duration " + (EndTime - StartTime).ToString("g"));
            }
            catch (System.Exception ex)
            {
                //LogToUser("Error when running the Hierarchy task on Qlikview Server: {0}", ex.Message);
                Trace.WriteLine(string.Format("An error occurred running the Hierarchy task... {0}", ex));
                throw new ReductionRunnerException(string.Format("An error occurred running the Hierarchy task... {0}", ex));
            }*/
        }
    }
}

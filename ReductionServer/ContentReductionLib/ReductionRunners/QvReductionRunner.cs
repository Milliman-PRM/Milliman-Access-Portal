/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: The reduction runner class for Qlikview content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using AuditLogLib;
using QmsApi;
using Newtonsoft.Json;

namespace ContentReductionLib.ReductionRunners
{
    internal class QvReductionRunner : ReductionRunnerBase
    {
        // TODO Get these from configuration
        const string QmsUrl = "http://indy-qvtest01:4799/QMS/Service";
        AuditLogger Logger = new AuditLogger();  // Exception if static AuditLogger.Config is not initialized (should be done globally for process)

        internal CancellationToken _CancellationToken { private get; set; }

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
        ReductionJobResult TaskResultObj { get; set; } = new ReductionJobResult();

        /// <summary>
        /// TODO This is a MapDb type, need an independent type to represent request properties agnostic of queue type
        /// </summary>
        private ContentReductionTask _QueueTask;
        internal ContentReductionTask QueueTask
        {
            set
            {
                TaskResultObj.TaskId = value.Id;
                _QueueTask = value;
            }
            private get
            {
                return _QueueTask;
            }
        }

        private DocumentFolder SourceDocFolder { get; set; } = null;

        private string WorkingFolderRelative { get; set; } = string.Empty;

        private ServiceInfo QdsServiceInfo { get; set; } = null;

        private string MasterFileName { get { return "Master.qvw"; } }
        #endregion

        internal async override Task<ReductionJobResult> ExecuteReduction(CancellationToken cancellationToken)
        {
            _CancellationToken = cancellationToken;
            MethodBase Method = MethodBase.GetCurrentMethod();

            try
            {
                ValidateThisInstance();
                _CancellationToken.ThrowIfCancellationRequested();

                PreTaskSetup();
                _CancellationToken.ThrowIfCancellationRequested();

                await ExtractReductionHierarchy();
                _CancellationToken.ThrowIfCancellationRequested();

                await CreateReducedContent();
                _CancellationToken.ThrowIfCancellationRequested();

                DistributeResults();

                TaskResultObj.Status = ReductionJobStatusEnum.Success;
            }
            catch (OperationCanceledException e)
            {
                TaskResultObj.Status = ReductionJobStatusEnum.Canceled;
                Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} {e.Message}");
            }
            catch (System.Exception e)
            {
                TaskResultObj.Status = ReductionJobStatusEnum.Error;
                Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} {e.Message}");
            }
            finally
            {
                // Never touch TaskResultObj.Status in the finally block. This value should always be finalized before we get here. 
                Cleanup();
            }

            return TaskResultObj;
        }

        internal override void ValidateThisInstance()
        {
            if (QueueTask == null ||
                Logger == null ||
                QdsServiceInfo == null ||
                QdsServiceInfo.ID == Guid.Empty ||
                SourceDocFolder == null ||
                !Directory.Exists(SourceDocFolder.General.Path) ||
                _CancellationToken == null)
            {
                MethodBase Method = MethodBase.GetCurrentMethod();
                string Msg = $"Error in {Method.ReflectedType.Name}.{Method.Name}";
                throw new System.Exception(Msg);
            }
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

            // Create and run Qlikview publisher (QDS) task
            TaskInfo Info = await CreateHierarchyExtractionQdsTask();
            await RunQdsTask(Info);

            // Clean up
            await DeleteQdsTask(Info);
            File.Delete(AncillaryScriptFilePath);

            #region Build hierarchy json output
            string ReductionSchemeFilePath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, "reduction.scheme.csv");
            try
            {
                ExtractedHierarchy Hierarchy = new ExtractedHierarchy();

                foreach (string Line in File.ReadLines(ReductionSchemeFilePath))
                {
                    // First line is csv header
                    if (Line.Contains("FieldName"))
                    {
                        continue;
                    }

                    string[] Fields = Line.Split(new char[] { ',' }, StringSplitOptions.None);

                    ExtractedField NewField = new ExtractedField { FieldName = Fields[1], DisplayName = Fields[2], Delimiter = Fields[4] };
                    NewField.ValueStructure = Fields[3];

                    string ValuesFileName = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, "fieldvalues." + Fields[1] + ".csv");
                    NewField.FieldValues = File.ReadAllLines(ValuesFileName).ToList();

                    File.Delete(ValuesFileName);

                    Hierarchy.Fields.Add(NewField);
                }

                TaskResultObj.ExtractedHierarchy = Hierarchy;
            }
            catch (System.Exception e)
            {
                Trace.WriteLine($"Error converting file {ReductionSchemeFilePath} to json output.  Details:" + Environment.NewLine + e.Message);
            }
            #endregion

            File.Delete(ReductionSchemeFilePath);
            foreach (string LogFile in Directory.EnumerateFiles(Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative), MasterFileName + "*.log"))
            {
                try
                {
                    File.Delete(LogFile);
                }
                catch (System.Exception e)
                {
                    Trace.WriteLine($"Failed to delete Qlikview task log file {LogFile}:" + Environment.NewLine + e.Message);
                    throw;
                }
            }

            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed ExtractReductionHierarchy");

            return true;
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private async Task<bool> CreateReducedContent()
        {
            // TODO create a properly typed object here
            ContentReductionHierarchy<ReductionFieldValueSelection> Selections = JsonConvert.DeserializeObject<ContentReductionHierarchy<ReductionFieldValueSelection>>(QueueTask.SelectionCriteria);

            TaskInfo Info = await CreateReductionQdsTask(Selections);
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} started CreateReducedContent");
            Thread.Sleep(2500);
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed CreateReducedContent");

            return true;
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private bool DistributeResults()
        {
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} started DistributeResults");
            Thread.Sleep(2500);
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} completed DistributeResults");

            return true;
        }

        /// <summary>
        /// Complete this
        /// </summary>
        private bool Cleanup()
        {
            Trace.WriteLine($"Task {QueueTask.Id.ToString()} started Cleanup");
            Thread.Sleep(2500);
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

        private async Task<QmsApi.TaskInfo> CreateReductionQdsTask(ContentReductionHierarchy<ReductionFieldValueSelection> Selections)
        {
            //TODO debug this function
            string TaskDateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Create new task object
            DocumentTask NewDocumentTask = new DocumentTask();

            NewDocumentTask.ID = Guid.NewGuid();
            NewDocumentTask.QDSID = QdsServiceInfo.ID;
            NewDocumentTask.Document = GetSourceDocumentNode(MasterFileName, WorkingFolderRelative);

            #region general
            NewDocumentTask.Scope |= QmsApi.DocumentTaskScope.General;
            NewDocumentTask.General = new QmsApi.DocumentTask.TaskGeneral();
            NewDocumentTask.General.Enabled = true;
            NewDocumentTask.General.TaskName = $"Reload and partial reduction task {NewDocumentTask.ID.ToString("D")} at {TaskDateTimeStamp}";
            NewDocumentTask.General.TaskDescription = $"Automatically generated by ReductionService at {TaskDateTimeStamp}";
            #endregion

            #region reduce
            int NumSelectedValues = 0;
            Selections.Fields.ForEach(f => NumSelectedValues += f.Values.Count(v => v.SelectionStatus));

            NewDocumentTask.Scope |= QmsApi.DocumentTaskScope.Reduce;
            NewDocumentTask.Reduce = new QmsApi.DocumentTask.TaskReduce();
            NewDocumentTask.Reduce.DocumentNameTemplate = Path.GetFileNameWithoutExtension(MasterFileName) + ".reduced." + Path.GetExtension(MasterFileName);
            NewDocumentTask.Reduce.Static = new QmsApi.DocumentTask.TaskReduce.TaskReduceStatic();
            NewDocumentTask.Reduce.Static.Reductions = new QmsApi.TaskReduction[NumSelectedValues];

            int Index = 0;
            foreach (var Field in Selections.Fields)
            {
                foreach (var Val in Field.Values)
                {
                    Trace.WriteLine(string.Format($"Reduction task: assigning selection for field <{Field.FieldName}> with value <{Val.Value}>")); // TODO delete this
                    NewDocumentTask.Reduce.Static.Reductions[Index] = new QmsApi.TaskReduction();
                    NewDocumentTask.Reduce.Static.Reductions[Index].Type = QmsApi.TaskReductionType.ByField;
                    NewDocumentTask.Reduce.Static.Reductions[Index].Field = new QmsApi.TaskReduction.TaskReductionField();
                    NewDocumentTask.Reduce.Static.Reductions[Index].Field.Name = Field.FieldName;
                    NewDocumentTask.Reduce.Static.Reductions[Index].Field.Value = Val.Value;
                    NewDocumentTask.Reduce.Static.Reductions[Index].Field.IsNumeric = double.TryParse(Val.Value, out _);
                    Index++;
                }
            }

            NewDocumentTask.Reduce.Dynamic = new QmsApi.DocumentTask.TaskReduce.TaskReduceDynamic();
            #endregion

            #region distribute
            NewDocumentTask.Scope |= QmsApi.DocumentTaskScope.Distribute;
            NewDocumentTask.Distribute = new QmsApi.DocumentTask.TaskDistribute();
            NewDocumentTask.Distribute.Output = new QmsApi.DocumentTask.TaskDistribute.TaskDistributeOutput();
            NewDocumentTask.Distribute.Output.Type = QmsApi.TaskDistributionOutputType.QlikViewDocument;

            NewDocumentTask.Distribute.Static = new QmsApi.DocumentTask.TaskDistribute.TaskDistributeStatic();
            NewDocumentTask.Distribute.Static.DistributionEntries = new QmsApi.TaskDistributionEntry[1];
            NewDocumentTask.Distribute.Static.DistributionEntries[0] = new QmsApi.TaskDistributionEntry();
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Recipients = new QmsApi.DirectoryServiceObject[1];
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Recipients[0] = new QmsApi.DirectoryServiceObject();
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Recipients[0].Type = QmsApi.DirectoryServiceObjectType.Authenticated;
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Destination = new QmsApi.TaskDistributionDestination();
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Destination.Folder = new QmsApi.TaskDistributionDestination.TaskDistributionDestinationFolder();
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Destination.Type = QmsApi.TaskDistributionDestinationType.Folder;
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Destination.Folder.Name = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);

            NewDocumentTask.Distribute.Notification = new QmsApi.DocumentTask.TaskDistribute.TaskDistributeNotification();
            NewDocumentTask.Distribute.Notification.SendNotificationEmail = false;

            NewDocumentTask.Distribute.Dynamic = new QmsApi.DocumentTask.TaskDistribute.TaskDistributeDynamic();
            NewDocumentTask.Distribute.Dynamic.IdentityType = QmsApi.UserIdentityValueType.DisplayName;
            NewDocumentTask.Distribute.Dynamic.Destinations = new QmsApi.TaskDistributionDestination[0];
            #endregion

            #region reload
            NewDocumentTask.Scope |= QmsApi.DocumentTaskScope.Reload;
            NewDocumentTask.Reload = new QmsApi.DocumentTask.TaskReload();
            //if (drop_required)
            //    task_reduction.Reload.Mode = QmsApi.TaskReloadMode.Partial; //this will result in tables being dropped as defined by the drop table processor
            //else
                  NewDocumentTask.Reload.Mode = QmsApi.TaskReloadMode.None;
            #endregion

            #region trigger
            //documentTask.DocumentInfo = new DocumentTask.TaskDocumentInfo();
            NewDocumentTask.Triggering = new QmsApi.DocumentTask.TaskTriggering();
            NewDocumentTask.Triggering.ExecutionAttempts = 1;
            NewDocumentTask.Triggering.ExecutionTimeout = 1440;
            NewDocumentTask.Triggering.TaskDependencies = new QmsApi.TaskInfo[] { };

            NewDocumentTask.Triggering.Triggers = new QmsApi.ScheduleTrigger[]
            {
                new QmsApi.ScheduleTrigger
                {
                    Enabled = true,
                    Type = QmsApi.TaskTriggerType.OnceTrigger,
                    StartAt = DateTime.Now.AddSeconds(5.0), //don't schedule for now, it ignores if in past
                }
            };
            #endregion

            IQMS QmsClient = QmsClientCreator.New(QmsUrl);

            await QmsClient.SaveDocumentTaskAsync(NewDocumentTask);
            QmsApi.TaskInfo ReturnTaskInfo = await QmsClient.FindTaskAsync(QdsServiceInfo.ID, QmsApi.TaskType.DocumentTask, NewDocumentTask.General.TaskName);

            Trace.WriteLine($"Task with ID '{NewDocumentTask.ID.ToString("N")}' successfully saved");

            return ReturnTaskInfo;
        }

        /// <summary>
        /// Manages the execution of a QDS task and monitors the completion of that task
        /// </summary>
        /// <param name="TInfo"></param>
        /// <returns></returns>
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
                    throw new System.Exception($"Qlikview publisher failed to start task {TInfo.ID} before timeout");
                }

                await QmsClient.RunTaskAsync(TInfo.ID);
                Thread.Sleep(250);

                Status = await QmsClient.GetTaskStatusAsync(TInfo.ID, TaskStatusScope.Extended);
            } while (Status == null || Status.Extended == null || string.IsNullOrEmpty(Status.Extended.StartTime));
            Trace.WriteLine($"In QvReductionRunner.RunQdsTask() task {TInfo.ID} started running after {DateTime.Now - StartTime}");

            // Wait for started task to finish
            DateTime RunningStartTime = DateTime.Now;
            do
            {
                if (DateTime.Now - RunningStartTime > MaxElapsedRun)
                {
                    throw new System.Exception($"Qlikview publisher failed to finish task {TInfo.ID} before timeout");
                }

                Thread.Sleep(250);

                Status = await QmsClient.GetTaskStatusAsync(TInfo.ID, TaskStatusScope.Extended);
            } while (Status == null || Status.Extended == null || !DateTime.TryParse(Status.Extended.FinishedTime, out DateTime dummy));
            Trace.WriteLine($"In QvReductionRunner.RunQdsTask() task {TInfo.ID} finished running after {DateTime.Now - RunningStartTime}");
        }

        private async Task<bool> DeleteQdsTask(QmsApi.TaskInfo TInfo)
        {
            IQMS QmsClient = QmsClientCreator.New(QmsUrl);
            QmsApi.TaskStatus Status = await QmsClient.GetTaskStatusAsync(TInfo.ID, TaskStatusScope.Extended);

            // null should indicate that the task doesn't exist
            bool Result = (Status == null) || await QmsClient.DeleteTaskAsync(TInfo.ID, TInfo.Type);

            return Result;
        }

    }
}

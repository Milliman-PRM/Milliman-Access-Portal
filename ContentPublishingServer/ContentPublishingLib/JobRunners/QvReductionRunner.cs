/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: The reduction runner class for Qlikview content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using AuditLogLib;
using AuditLogLib.Services;
using QlikviewLib.Qms;
using MapCommonLib;

namespace ContentPublishingLib.JobRunners
{
    public class QvReductionRunner : RunnerBase
    {
        private string QmsUrl = null;

        /// <summary>
        /// Constructor, sets up starting conditions that are associated with the system configuration rather than this specific task.
        /// </summary>
        public QvReductionRunner()
        {
            // Initialize members
            QmsUrl = Configuration.ApplicationConfiguration["IQmsUrl"];

            IQMS Client = QmsClientCreator.New(QmsUrl);
            QdsServiceInfo = Client.GetServicesAsync(ServiceTypes.QlikViewDistributionService).Result[0];
            SourceDocFolder = Client.GetSourceDocumentFoldersAsync(QdsServiceInfo.ID, DocumentFolderScope.All).Result[1];  // TODO Get this index right for production
        }

        #region Member properties
        public ReductionJobDetail JobDetail { get; set; } = new ReductionJobDetail();

        private DocumentFolder SourceDocFolder { get; set; } = null;

        private string WorkingFolderRelative { get; set; } = string.Empty;

        private ServiceInfo QdsServiceInfo { get; set; } = null;

        private string MasterFileName { get { return Path.GetFileName(JobDetail.Request.MasterFilePath); } }

        private string ReducedFileName { get { return Path.ChangeExtension(MasterFileName, $".reduced{Path.GetExtension(MasterFileName)}"); } }

        private DocumentNode MasterDocumentNode { get; set; } = null;

        private DocumentNode ReducedDocumentNode { get; set; } = null;
        #endregion

        /// <summary>
        /// Entry point for the execution of a reduction task.  Intended to be invoked as a Task by a JobMonitorBase derived object. 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ReductionJobDetail> Execute(CancellationToken cancellationToken)
        {
            if (AuditLog == null)
            {
                AuditLog = new AuditLogger();
            }

            _CancellationToken = cancellationToken;

            MethodBase Method = MethodBase.GetCurrentMethod();
            object DetailObj;
            AuditEvent Event;

            ReductionJobActionEnum[] SupportedJobActions = new ReductionJobActionEnum[] 
            {
                ReductionJobActionEnum.HierarchyOnly,
                ReductionJobActionEnum.HierarchyAndReduction
            };

            try
            {
                if (!SupportedJobActions.Contains(JobDetail.Request.JobAction))
                {
                    throw new ApplicationException($"QvReductionRunner.Execute() refusing to process job with unsupported requested action: {JobDetail.Request.JobAction.ToString()}");
                }
                else
                {
                    ValidateThisInstance();
                    _CancellationToken.ThrowIfCancellationRequested();

                    await PreTaskSetup();

                    #region Extract master content hierarchy
                    JobDetail.Result.MasterContentHierarchy = await ExtractReductionHierarchy(MasterDocumentNode);

                    DetailObj = new { ReductionJobId = JobDetail.TaskId.ToString(), JobAction = JobDetail.Request.JobAction, Hierarchy = JobDetail.Result.MasterContentHierarchy };
                    Event = AuditEvent.New("Reduction server", "Extraction of master content hierarchy succeeded", AuditEventId.HierarchyExtractionSucceeded, DetailObj);
                    AuditLog.Log(Event);
                    #endregion

                    _CancellationToken.ThrowIfCancellationRequested();

                    if (JobDetail.Request.JobAction == ReductionJobActionEnum.HierarchyAndReduction)
                    {
                        #region Create reduced content
                        await CreateReducedContent();

                        DetailObj = new { ReductionJobId = JobDetail.TaskId.ToString(), RequestedSelections = JobDetail.Request.SelectionCriteria };
                        Event = AuditEvent.New("Reduction server", "Creation of reduced content succeeded", AuditEventId.ContentReductionSucceeded, DetailObj);
                        AuditLog.Log(Event);
                        #endregion

                        _CancellationToken.ThrowIfCancellationRequested();

                        #region Extract reduced content hierarchy
                        JobDetail.Result.ReducedContentHierarchy = await ExtractReductionHierarchy(ReducedDocumentNode);

                        DetailObj = new { ReductionJobId = JobDetail.TaskId.ToString(), JobAction = JobDetail.Request.JobAction, Hierarchy = JobDetail.Result.ReducedContentHierarchy };
                        Event = AuditEvent.New("Reduction server", "Extraction of reduced content hierarchy succeeded", AuditEventId.HierarchyExtractionSucceeded, DetailObj);
                        AuditLog.Log(Event);
                        #endregion

                        _CancellationToken.ThrowIfCancellationRequested();

                        DistributeReducedContent();
                    }

                    JobDetail.Status = ReductionJobDetail.JobStatusEnum.Success;
                }
            }
            catch (OperationCanceledException e)
            {
                JobDetail.Status = ReductionJobDetail.JobStatusEnum.Canceled;
                Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} {e.Message}");
                JobDetail.Result.StatusMessage = e.Message;
            }
            catch (ApplicationException e)
            {
                JobDetail.Status = ReductionJobDetail.JobStatusEnum.Error;
                Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} {e.Message}");
                JobDetail.Result.StatusMessage = e.Message;
            }
            catch (System.Exception e)
            {
                JobDetail.Status = ReductionJobDetail.JobStatusEnum.Error;
                Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} {e.Message}");
                JobDetail.Result.StatusMessage = e.Message;
            }
            finally
            {
                // Don't touch TaskResultObj.Status in the finally block. This value should always be finalized before we get here. 
                Cleanup();
            }

            return JobDetail;
        }

        /// <summary>
        /// Validate as many as possible of the member properties
        /// </summary>
        internal void ValidateThisInstance()
        {
            string Msg = null;

            if (JobDetail == null || JobDetail.Request == null || JobDetail.Request.SelectionCriteria == null || JobDetail.Result == null)
            {
                Msg = "JobDetailObj, or a member of it, is null";
            }

            else if (QdsServiceInfo == null)
            {
                Msg = "QdsServiceInfo is null";
            }

            else if (QdsServiceInfo.ID == Guid.Empty)
            {
                Msg = "QdsServiceInfo.ID is Guid.Empty";
            }

            else if (SourceDocFolder == null)
            {
                Msg = "SourceDocFolder is null";
            }

            else if (JobDetail.Request.JobAction != ReductionJobActionEnum.HierarchyOnly 
                  && JobDetail.Request.SelectionCriteria.Count(v => v.Selected) == 0)
            {
                Msg = $"No selected field values are included in the reduction request";
            }

            else if (!Directory.Exists(SourceDocFolder.General.Path))
            {
                Msg = $"SourceDocFolder {SourceDocFolder.General.Path} not found";
            }

            else if (_CancellationToken == null)
            {
                Msg = "_CancellationToken is null";
            }

            if (!string.IsNullOrEmpty(Msg))
            {
                MethodBase Method = MethodBase.GetCurrentMethod();

                object DetailObj = new { ReductionJobId = JobDetail.TaskId.ToString(), Error = Msg};
                AuditEvent Event = AuditEvent.New("Reduction server", "Validation of processing prerequisites failed", AuditEventId.ReductionValidationFailed, DetailObj);
                AuditLog.Log(Event);

                Msg = $"Error in {Method.ReflectedType.Name}.{Method.Name}: {Msg}";

                throw new System.ApplicationException(Msg);
            }
        }

        /// <summary>
        /// Sets up the starting conditions that are unique to this specific task
        /// </summary>
        private async Task<bool> PreTaskSetup()
        {
            WorkingFolderRelative = JobDetail.TaskId.ToString();  // Folder is named for the task guid from the database
            string WorkingFolderAbsolute = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);
            string MasterFileDestinationPath = Path.Combine(WorkingFolderAbsolute, MasterFileName);

            try
            {
                if (Directory.Exists(WorkingFolderAbsolute))
                {
                    Directory.Delete(WorkingFolderAbsolute, true);
                }

                if (!File.Exists(JobDetail.Request.MasterFilePath))
                {
                    throw new ApplicationException($"Master file {JobDetail.Request.MasterFilePath} does not exist");
                }

                Directory.CreateDirectory(WorkingFolderAbsolute);
                File.Copy(JobDetail.Request.MasterFilePath, MasterFileDestinationPath);

                if (GlobalFunctions.GetFileChecksum(MasterFileDestinationPath) != JobDetail.Request.MasterContentChecksum)
                {
                    throw new ApplicationException("Master content file integrity check failed, mismatch of file hash");
                }

                MasterDocumentNode = await GetSourceDocumentNode(MasterFileName, WorkingFolderRelative);
            }
            catch (System.Exception e)
            {
                Trace.WriteLine($"QvReductionRunner.PreTaskSetup() failed to create folder {WorkingFolderAbsolute} or copy master file {JobDetail.Request.MasterFilePath} to {MasterFileDestinationPath}, {GlobalFunctions.LoggableExceptionString(e)}");
                throw;
            }

            if (MasterDocumentNode == null)
            {
                throw new ApplicationException("Failed to obtain DocumentNode object from Qlikview Publisher for master content file");
            }
            return true;
        }

        /// <summary>
        /// Extracts the reduction fields and corresponding values of a Qlikview content item
        /// </summary>
        private async Task<ExtractedHierarchy> ExtractReductionHierarchy(DocumentNode DocumentNodeArg)
        {
            ExtractedHierarchy ResultHierarchy = new ExtractedHierarchy();

            // Create ancillary script
            string AncillaryScriptFilePath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, "ancillary_script.txt");
            File.WriteAllText(AncillaryScriptFilePath, "LET DataExtraction=true();");

            // Create Qlikview publisher (QDS) task
            DocumentTask HierarchyTask = await CreateHierarchyExtractionQdsTask(DocumentNodeArg);

            // Run Qlikview publisher (QDS) task
            try
            {
                await RunQdsTask(HierarchyTask);
            }
            finally
            {
                // Clean up
                File.Delete(AncillaryScriptFilePath);
            }

            #region Build hierarchy json output
            string ReductionSchemeFilePath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, "reduction.scheme.csv");

            try
            {
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
                    NewField.FieldValues = File.ReadAllLines(ValuesFileName).Skip(1).ToList();  // skip because the first line is the field name

                    File.Delete(ValuesFileName);

                    ResultHierarchy.Fields.Add(NewField);
                }
            }
            catch (System.Exception e)
            {
                Trace.WriteLine($"Error converting file {ReductionSchemeFilePath} to json output.  Details:" + Environment.NewLine + e.Message);

                // TODO may need to log more issues, like if the Qlikview task processing fails
                object DetailObj = new { ReductionJobId = JobDetail.TaskId.ToString(), ExceptionMessage = e.Message };
                AuditEvent Event = AuditEvent.New("Reduction server", "Extraction of hierarchy failed", AuditEventId.HierarchyExtractionFailed, DetailObj);
                AuditLog.Log(Event);
            }
            finally
            {
                File.Delete(ReductionSchemeFilePath);
            }
            #endregion

            foreach (string LogFile in Directory.EnumerateFiles(Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative), DocumentNodeArg.Name + "*.log"))
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

            Trace.WriteLine($"Task {JobDetail.TaskId.ToString()} completed ExtractReductionHierarchy");

            return ResultHierarchy;
        }

        /// <summary>
        /// Create a reduced version of the master content file based on requested field value selections
        /// </summary>
        private async Task CreateReducedContent()
        {
            // Validate the selected field name(s) exists in the extracted hierarchy
            foreach (var SelectedFieldValue in JobDetail.Request.SelectionCriteria)
            {
                if (!JobDetail.Result.MasterContentHierarchy.Fields.Any(f => f.FieldName == SelectedFieldValue.FieldName))
                {
                    string Msg = $"The requested reduction field <{SelectedFieldValue.FieldName}> is not found in the reduction hierarchy";
                    object DetailObj = new { ReductionJobId = JobDetail.TaskId.ToString(), Error = Msg };
                    AuditEvent Event = AuditEvent.New("Reduction server", "Creation of reduced content file failed", AuditEventId.ContentReductionFailed, DetailObj);
                    AuditLog.Log(Event);
                    Trace.WriteLine(Msg);
                    throw new ApplicationException(Msg);
                }
            }

            // Validate that there is at least one selected value that exists in the hierarchy. 
            if (!JobDetail.Request.SelectionCriteria.Any(s => s.Selected &&
                                                              JobDetail.Result.MasterContentHierarchy.Fields.Any(f => f.FieldName == s.FieldName && f.FieldValues.Contains(s.FieldValue))))
            {
                string Msg = $"No requested selections exist in the master hierarchy";
                object DetailObj = new { ReductionJobId = JobDetail.TaskId.ToString(), RequestesSelections = JobDetail.Request.SelectionCriteria, Error = Msg };
                AuditEvent Event = AuditEvent.New("Reduction server", "Creation of reduced content file failed", AuditEventId.ContentReductionFailed, DetailObj);
                AuditLog.Log(Event);
                Trace.WriteLine(Msg);
                throw new ApplicationException(Msg);
            }

            // Create Qlikview publisher (QDS) task
            DocumentTask ReductionTask = await CreateReductionQdsTask(JobDetail.Request.SelectionCriteria);

            // Run Qlikview publisher (QDS) task
            await RunQdsTask(ReductionTask);

            ReducedDocumentNode = await GetSourceDocumentNode(ReducedFileName, WorkingFolderRelative);

            if (ReducedDocumentNode == null)
            {
                Trace.WriteLine($"Failed to get DocumentNode for file {ReducedFileName} in folder {SourceDocFolder.General.Path}\\{WorkingFolderRelative}");
            }

            Trace.WriteLine($"Task {JobDetail.TaskId.ToString()} completed CreateReducedContent");
        }

        /// <summary>
        /// Makes the outcome of the content reduction operation accessible to the main application
        /// </summary>
        private void DistributeReducedContent()
        {
            string ApplicationDataExchangeFolder = Path.GetDirectoryName(JobDetail.Request.MasterFilePath);
            string WorkingFolderAbsolute = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);

            string ReducedFile = Directory.GetFiles(WorkingFolderAbsolute, ReducedFileName).Single();
            string CopyDestinationPath = Path.Combine(ApplicationDataExchangeFolder, Path.GetFileName(ReducedFile));

            File.Copy(ReducedFile, CopyDestinationPath, true);
            JobDetail.Result.ReducedContentFileChecksum = GlobalFunctions.GetFileChecksum(CopyDestinationPath);
            JobDetail.Result.ReducedContentFilePath = CopyDestinationPath;

            Trace.WriteLine($"Task {JobDetail.TaskId.ToString()} completed DistributeReducedContent");
        }

        /// <summary>
        /// Remove all temporary artifacts of the entire process
        /// </summary>
        private bool Cleanup()
        {
            string WorkingFolderAbsolute = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);
            if (!string.IsNullOrWhiteSpace(WorkingFolderRelative) && Directory.Exists(WorkingFolderAbsolute))
            {
                Directory.Delete(WorkingFolderAbsolute, true);
            }

            Trace.WriteLine($"Task {JobDetail.TaskId.ToString()} completed Cleanup");

            return true;
        }

        /// <summary>
        /// Returns Qv DocumentNode object corresponding to the requested file 
        /// </summary>
        /// <param name="RequestedFileName"></param>
        /// <param name="RequestedRelativeFolder">Path relative to the selected source documents folder.</param>
        /// <returns></returns>
        private async Task<DocumentNode> GetSourceDocumentNode(string RequestedFileName, string RequestedRelativeFolder)
        {
            DocumentNode DocNode = null;

            IQMS QmsClient = QmsClientCreator.New(QmsUrl);

            DocumentNode[] AllDocNodes = new DocumentNode[0];
            DateTime Start = DateTime.Now;
            while (DocNode == null && (DateTime.Now - Start) < new TimeSpan(0, 1, 10))  // QV server seems to poll for files every minute
            {
                Thread.Sleep(500);
                AllDocNodes = await QmsClient.GetSourceDocumentNodesAsync(QdsServiceInfo.ID, SourceDocFolder.ID, RequestedRelativeFolder);
                DocNode = AllDocNodes.SingleOrDefault(dn => dn.FolderID == SourceDocFolder.ID
                                                            && dn.Name == RequestedFileName
                                                            && dn.RelativePath == RequestedRelativeFolder);
            }

            if (DocNode == null)
            {
                Trace.WriteLine(string.Format($"Did not find SourceDocument '{MasterFileName}' in subfolder {RequestedRelativeFolder} of source documents folder {SourceDocFolder.General.Path}"));
            }

            return DocNode;
        }

        /// <summary>
        /// Creates and stores a Qlikview publisher task to extract the selection hierarchy info for a QV document
        /// </summary>
        /// <param name="DocNodeArg"></param>
        /// <returns></returns>
        private async Task<DocumentTask> CreateHierarchyExtractionQdsTask(DocumentNode DocNodeArg)
        {
            string TaskDateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Create new task object
            DocumentTask NewDocumentTask = new DocumentTask();

            NewDocumentTask.ID = Guid.NewGuid();
            NewDocumentTask.QDSID = QdsServiceInfo.ID;
            NewDocumentTask.Document = DocNodeArg;

            #region General Tab
            NewDocumentTask.Scope |= DocumentTaskScope.General;
            NewDocumentTask.General = new DocumentTask.TaskGeneral();
            NewDocumentTask.General.Enabled = true;
            NewDocumentTask.General.TaskName = $"Hierarchy extraction for task {JobDetail.TaskId.ToString("D")} at {TaskDateTimeStamp}";
            NewDocumentTask.General.TaskDescription = $"Automatically generated by ReductionService at {TaskDateTimeStamp}";
            #endregion

            #region Reload Tab
            NewDocumentTask.Scope |= DocumentTaskScope.Reload;
            NewDocumentTask.Reload = new DocumentTask.TaskReload();
            NewDocumentTask.Reload.Mode = TaskReloadMode.Partial;
            #endregion

            #region Reduce Tab
            NewDocumentTask.Scope |= DocumentTaskScope.Reduce;
            NewDocumentTask.Reduce = new DocumentTask.TaskReduce();
            NewDocumentTask.Reduce.DocumentNameTemplate = string.Empty;
            NewDocumentTask.Reduce.Static = new DocumentTask.TaskReduce.TaskReduceStatic();
            NewDocumentTask.Reduce.Static.Reductions = new TaskReduction[] { new TaskReduction() };
            NewDocumentTask.Reduce.Static.Reductions[0].Type = new TaskReductionType();
            NewDocumentTask.Reduce.Dynamic = new DocumentTask.TaskReduce.TaskReduceDynamic();
            NewDocumentTask.Reduce.Dynamic.Type = new TaskReductionType();
            #endregion

            #region Trigger Tab
            NewDocumentTask.Scope |= DocumentTaskScope.Triggering;
            NewDocumentTask.Triggering = new DocumentTask.TaskTriggering();
            NewDocumentTask.Triggering.ExecutionAttempts = 1;
            NewDocumentTask.Triggering.ExecutionTimeout = 1440;
            NewDocumentTask.Triggering.TaskDependencies = new TaskInfo[] { };
            NewDocumentTask.Triggering.Triggers = new ScheduleTrigger[] {
                    new ScheduleTrigger()
                };
            NewDocumentTask.Triggering.Triggers[0].Enabled = true;
            NewDocumentTask.Triggering.Triggers[0].Type = TaskTriggerType.OnceTrigger;
            ((ScheduleTrigger)NewDocumentTask.Triggering.Triggers[0]).StartAt = DateTime.Now;
            #endregion

            return NewDocumentTask;
        }

        /// <summary>
        /// Creates and stores a Qlikview publisher task to produce a reduced version of a master QV document
        /// </summary>
        /// <param name="Selections"></param>
        /// <returns></returns>
        private async Task<DocumentTask> CreateReductionQdsTask(IEnumerable<FieldValueSelection> Selections)
        {
            //TODO debug this function
            string TaskDateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Create new task object
            DocumentTask NewDocumentTask = new DocumentTask();

            NewDocumentTask.ID = Guid.NewGuid();
            NewDocumentTask.QDSID = QdsServiceInfo.ID;
            NewDocumentTask.Document = MasterDocumentNode;

            #region general
            NewDocumentTask.Scope |= DocumentTaskScope.General;
            NewDocumentTask.General = new DocumentTask.TaskGeneral();
            NewDocumentTask.General.Enabled = true;
            NewDocumentTask.General.TaskName = $"Reload and partial reduction task {NewDocumentTask.ID.ToString("D")} at {TaskDateTimeStamp}";
            NewDocumentTask.General.TaskDescription = $"Automatically generated by ReductionService at {TaskDateTimeStamp}";
            #endregion

            #region reduce
            int NumSelectedValues = Selections.Count(v => v.Selected);

            NewDocumentTask.Scope |= DocumentTaskScope.Reduce;
            NewDocumentTask.Reduce = new DocumentTask.TaskReduce();
            NewDocumentTask.Reduce.DocumentNameTemplate = Path.GetFileNameWithoutExtension(ReducedFileName);
            NewDocumentTask.Reduce.Static = new DocumentTask.TaskReduce.TaskReduceStatic();
            NewDocumentTask.Reduce.Static.Reductions = new TaskReduction[NumSelectedValues];

            int Index = 0;
            foreach (FieldValueSelection FieldVal in Selections.Where(v => v.Selected))
            {
                Trace.WriteLine(string.Format($"Reduction task: assigning selection for field <{FieldVal.FieldName}> with value <{FieldVal.FieldValue}>")); // TODO delete this
                NewDocumentTask.Reduce.Static.Reductions[Index] = new TaskReduction();
                NewDocumentTask.Reduce.Static.Reductions[Index].Type = TaskReductionType.ByField;
                NewDocumentTask.Reduce.Static.Reductions[Index].Field = new TaskReduction.TaskReductionField();
                NewDocumentTask.Reduce.Static.Reductions[Index].Field.Name = FieldVal.FieldName;
                NewDocumentTask.Reduce.Static.Reductions[Index].Field.Value = FieldVal.FieldValue;
                NewDocumentTask.Reduce.Static.Reductions[Index].Field.IsNumeric = double.TryParse(FieldVal.FieldValue, out _);
                Index++;
            }

            NewDocumentTask.Reduce.Dynamic = new DocumentTask.TaskReduce.TaskReduceDynamic();
            #endregion

            #region distribute
            NewDocumentTask.Scope |= DocumentTaskScope.Distribute;
            NewDocumentTask.Distribute = new DocumentTask.TaskDistribute();
            NewDocumentTask.Distribute.Output = new DocumentTask.TaskDistribute.TaskDistributeOutput();
            NewDocumentTask.Distribute.Output.Type = TaskDistributionOutputType.QlikViewDocument;

            NewDocumentTask.Distribute.Static = new DocumentTask.TaskDistribute.TaskDistributeStatic();
            NewDocumentTask.Distribute.Static.DistributionEntries = new TaskDistributionEntry[1];
            NewDocumentTask.Distribute.Static.DistributionEntries[0] = new TaskDistributionEntry();
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Recipients = new DirectoryServiceObject[1];
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Recipients[0] = new DirectoryServiceObject();
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Recipients[0].Type = DirectoryServiceObjectType.Authenticated;
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Destination = new TaskDistributionDestination();
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Destination.Folder = new TaskDistributionDestination.TaskDistributionDestinationFolder();
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Destination.Type = TaskDistributionDestinationType.Folder;
            NewDocumentTask.Distribute.Static.DistributionEntries[0].Destination.Folder.Name = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);

            NewDocumentTask.Distribute.Notification = new DocumentTask.TaskDistribute.TaskDistributeNotification();
            NewDocumentTask.Distribute.Notification.SendNotificationEmail = false;

            NewDocumentTask.Distribute.Dynamic = new DocumentTask.TaskDistribute.TaskDistributeDynamic();
            NewDocumentTask.Distribute.Dynamic.IdentityType = UserIdentityValueType.DisplayName;
            NewDocumentTask.Distribute.Dynamic.Destinations = new TaskDistributionDestination[0];
            #endregion

            #region reload
            NewDocumentTask.Scope |= DocumentTaskScope.Reload;
            NewDocumentTask.Reload = new DocumentTask.TaskReload();
            // Note: following conditional is leftover from Millframe, left here as a reminder of previously supported functionality
            //if (drop_required)
            //    task_reduction.Reload.Mode = TaskReloadMode.Partial; //this will result in tables being dropped as defined by the drop table processor
            //else
                  NewDocumentTask.Reload.Mode = TaskReloadMode.None;
            #endregion

            #region trigger
            //documentTask.DocumentInfo = new DocumentTask.TaskDocumentInfo();
            NewDocumentTask.Triggering = new DocumentTask.TaskTriggering();
            NewDocumentTask.Triggering.ExecutionAttempts = 1;
            NewDocumentTask.Triggering.ExecutionTimeout = 1440;
            NewDocumentTask.Triggering.TaskDependencies = new TaskInfo[] { };

            NewDocumentTask.Triggering.Triggers = new ScheduleTrigger[]
            {
                new ScheduleTrigger
                {
                    Enabled = true,
                    Type = TaskTriggerType.OnceTrigger,
                    StartAt = DateTime.Now.AddSeconds(5.0), //don't schedule for now, it ignores if in past
                }
            };
            #endregion

            return NewDocumentTask;
        }

        /// <summary>
        /// Manages the execution of a QDS task and monitors the completion of that task
        /// </summary>
        /// <param name="TInfo"></param>
        /// <returns></returns>
        private async Task RunQdsTask(DocumentTask DocTask)
        {
            // TODO make these configurable?
            TimeSpan MaxStartDelay = new TimeSpan(0, 5, 0);
            TimeSpan MaxElapsedRun = new TimeSpan(0, 5, 0);
            int PublisherPollingIntervalMs = 250;

            QlikviewLib.Qms.TaskStatus Status;

            // Save the task to Qlikview server
            IQMS QmsClient = QmsClientCreator.New(QmsUrl);
            await QmsClient.SaveDocumentTaskAsync(DocTask);
            TaskInfo TInfo = await QmsClient.FindTaskAsync(QdsServiceInfo.ID, TaskType.DocumentTask, DocTask.General.TaskName);
            Guid TaskIdGuid = TInfo.ID;
            Trace.WriteLine($"QDS task with ID '{TaskIdGuid.ToString("D")}' successfully saved");

            try
            {
                // Get the task started, this generally requires more than one call to RunTaskAsync
                DateTime StartTime = DateTime.Now;
                do
                {
                    if (DateTime.Now - StartTime > MaxStartDelay)
                    {
                        throw new System.Exception($"Qlikview publisher failed to start task {TaskIdGuid.ToString("D")} before timeout");
                    }

                    QmsClient = QmsClientCreator.New(QmsUrl);
                    await QmsClient.RunTaskAsync(TaskIdGuid);
                    Thread.Sleep(PublisherPollingIntervalMs);

                    Status = await QmsClient.GetTaskStatusAsync(TaskIdGuid, TaskStatusScope.All);
                } while (Status == null || Status.Extended == null || string.IsNullOrEmpty(Status.Extended.StartTime));

                Trace.WriteLine($"In QvReductionRunner.RunQdsTask() task {TaskIdGuid.ToString("D")} started running after {DateTime.Now - StartTime}");

                // Wait for started task to finish
                DateTime RunningStartTime = DateTime.Now;
                do
                {
                    if (DateTime.Now - RunningStartTime > MaxElapsedRun)
                    {
                        throw new System.Exception($"Qlikview publisher failed to finish task {TaskIdGuid.ToString("D")} before timeout");
                    }

                    Thread.Sleep(PublisherPollingIntervalMs);

                    QmsClient = QmsClientCreator.New(QmsUrl);
                    Status = await QmsClient.GetTaskStatusAsync(TaskIdGuid, TaskStatusScope.All);
                } while (Status == null || Status.Extended == null || !DateTime.TryParse(Status.Extended.FinishedTime, out _));
                Trace.WriteLine($"In QvReductionRunner.RunQdsTask() task {TaskIdGuid.ToString("D")} finished running after {DateTime.Now - RunningStartTime}");

                if (Status.General.Status == TaskStatusValue.Failed)
                {
                    throw new ApplicationException($"Qlikview server error while processing task {TaskIdGuid.ToString("D")}:{Environment.NewLine}{Status.Extended.LastLogMessages}");
                }
            }
            finally
            {
                // Clean up
                QmsClient = QmsClientCreator.New(QmsUrl);
                Status = await QmsClient.GetTaskStatusAsync(TaskIdGuid, TaskStatusScope.All);

                // null would indicate that the task doesn't exist
                if (Status != null)
                {
                    await QmsClient.DeleteTaskAsync(TaskIdGuid, TInfo.Type);
                }
            }
        }

    }
}

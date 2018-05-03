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
using QmsApi;
using MapCommonLib;

namespace ContentReductionLib.ReductionRunners
{
    public class QvReductionRunner : ReductionRunnerBase
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
        internal CancellationToken _CancellationToken { private get; set; }

        public ReductionJobDetail JobDetail { get; set; } = new ReductionJobDetail();

        private DocumentFolder SourceDocFolder { get; set; } = null;

        private string WorkingFolderRelative { get; set; } = string.Empty;

        private ServiceInfo QdsServiceInfo { get; set; } = null;

        private string MasterFileName { get { return Path.GetFileName(JobDetail.Request.MasterFilePath); } }

        private string ReducedFileName { get { return Path.ChangeExtension(MasterFileName, $".reduced{Path.GetExtension(MasterFileName)}"); } }

        private DocumentNode MasterDocumentNode { get; set; } = null;

        private DocumentNode ReducedDocumentNode { get; set; } = null;

        private IAuditLogger AuditLog = null;
        #endregion

        #region Testing support
        public void SetTestAuditLogger(IAuditLogger LoggerArg)
        {
            AssertTesting();
            AuditLog = LoggerArg;
        }
        #endregion

        /// <summary>
        /// Entry point for the execution of a reduction task.  Intended to be invoked as a Task by a JobMonitorBase derived object. 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async override Task<ReductionJobDetail> Execute(CancellationToken cancellationToken)
        {
            if (AuditLog == null)
            {
                AuditLog = new AuditLogger();
            }

            _CancellationToken = cancellationToken;
            MethodBase Method = MethodBase.GetCurrentMethod();
            object DetailObj;
            AuditEvent Event;
            JobActionEnum[] SupportedJobActions = new JobActionEnum[] 
            {
                JobActionEnum.HierarchyOnly,
                JobActionEnum.HierarchyAndReduction
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

                    if (JobDetail.Request.JobAction == JobActionEnum.HierarchyAndReduction)
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

                    JobDetail.Result.Status = JobStatusEnum.Success;
                }
            }
            catch (OperationCanceledException e)
            {
                JobDetail.Result.Status = JobStatusEnum.Canceled;
                Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} {e.Message}");
                JobDetail.Result.StatusMessage = e.Message;
            }
            catch (ApplicationException e)
            {
                JobDetail.Result.Status = JobStatusEnum.Error;
                Trace.WriteLine($"{Method.ReflectedType.Name}.{Method.Name} {e.Message}");
                JobDetail.Result.StatusMessage = e.Message;
            }
            catch (System.Exception e)
            {
                JobDetail.Result.Status = JobStatusEnum.Error;
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
        internal override void ValidateThisInstance()
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

            else if (JobDetail.Request.SelectionCriteria.Count(v => v.Selected) == 0)
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
                Trace.WriteLine($"QvReductionRunner.PreTaskSetup() failed to create folder {WorkingFolderAbsolute} or copy master file {JobDetail.Request.MasterFilePath} to {MasterFileDestinationPath}, exception message:" + Environment.NewLine + e.Message);
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
            TaskInfo Info = await CreateHierarchyExtractionQdsTask(DocumentNodeArg);

            // Run Qlikview publisher (QDS) task
            await RunQdsTask(Info);

            // Clean up
            await DeleteQdsTask(Info);
            File.Delete(AncillaryScriptFilePath);

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
            #endregion

            File.Delete(ReductionSchemeFilePath);
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
            TaskInfo Info = await CreateReductionQdsTask(JobDetail.Request.SelectionCriteria);

            // Run Qlikview publisher (QDS) task
            await RunQdsTask(Info);

            // Clean up
            await DeleteQdsTask(Info);

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
            if (Directory.Exists(WorkingFolderAbsolute))
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
        private async Task<QmsApi.DocumentNode> GetSourceDocumentNode(string RequestedFileName, string RequestedRelativeFolder)
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
        private async Task<QmsApi.TaskInfo> CreateHierarchyExtractionQdsTask(DocumentNode DocNodeArg)
        {
            string TaskDateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Create new task object
            DocumentTask NewDocumentTask = new DocumentTask();

            NewDocumentTask.ID = Guid.NewGuid();
            NewDocumentTask.QDSID = QdsServiceInfo.ID;
            NewDocumentTask.Document = DocNodeArg;

            #region General Tab
            NewDocumentTask.Scope |= DocumentTaskScope.General;
            NewDocumentTask.General = new QmsApi.DocumentTask.TaskGeneral();
            NewDocumentTask.General.Enabled = true;
            NewDocumentTask.General.TaskName = $"Hierarchy extraction for task {JobDetail.TaskId.ToString("D")} at {TaskDateTimeStamp}";
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

            Trace.WriteLine($"Hierarchy extraction task with ID '{NewDocumentTask.ID.ToString("N")}' successfully saved");

            return ReturnTaskInfo;
        }

        /// <summary>
        /// Creates and stores a Qlikview publisher task to produce a reduced version of a master QV document
        /// </summary>
        /// <param name="Selections"></param>
        /// <returns></returns>
        private async Task<QmsApi.TaskInfo> CreateReductionQdsTask(IEnumerable<FieldValueSelection> Selections)
        {
            //TODO debug this function
            string TaskDateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Create new task object
            DocumentTask NewDocumentTask = new DocumentTask();

            NewDocumentTask.ID = Guid.NewGuid();
            NewDocumentTask.QDSID = QdsServiceInfo.ID;
            NewDocumentTask.Document = MasterDocumentNode;

            #region general
            NewDocumentTask.Scope |= QmsApi.DocumentTaskScope.General;
            NewDocumentTask.General = new QmsApi.DocumentTask.TaskGeneral();
            NewDocumentTask.General.Enabled = true;
            NewDocumentTask.General.TaskName = $"Reload and partial reduction task {NewDocumentTask.ID.ToString("D")} at {TaskDateTimeStamp}";
            NewDocumentTask.General.TaskDescription = $"Automatically generated by ReductionService at {TaskDateTimeStamp}";
            #endregion

            #region reduce
            int NumSelectedValues = Selections.Count(v => v.Selected);

            NewDocumentTask.Scope |= QmsApi.DocumentTaskScope.Reduce;
            NewDocumentTask.Reduce = new QmsApi.DocumentTask.TaskReduce();
            NewDocumentTask.Reduce.DocumentNameTemplate = Path.GetFileNameWithoutExtension(ReducedFileName);
            NewDocumentTask.Reduce.Static = new QmsApi.DocumentTask.TaskReduce.TaskReduceStatic();
            NewDocumentTask.Reduce.Static.Reductions = new QmsApi.TaskReduction[NumSelectedValues];

            int Index = 0;
            foreach (FieldValueSelection FieldVal in Selections)
            {
                Trace.WriteLine(string.Format($"Reduction task: assigning selection for field <{FieldVal.FieldName}> with value <{FieldVal.FieldValue}>")); // TODO delete this
                NewDocumentTask.Reduce.Static.Reductions[Index] = new QmsApi.TaskReduction();
                NewDocumentTask.Reduce.Static.Reductions[Index].Type = QmsApi.TaskReductionType.ByField;
                NewDocumentTask.Reduce.Static.Reductions[Index].Field = new QmsApi.TaskReduction.TaskReductionField();
                NewDocumentTask.Reduce.Static.Reductions[Index].Field.Name = FieldVal.FieldName;
                NewDocumentTask.Reduce.Static.Reductions[Index].Field.Value = FieldVal.FieldValue;
                NewDocumentTask.Reduce.Static.Reductions[Index].Field.IsNumeric = double.TryParse(FieldVal.FieldValue, out _);
                Index++;
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
            // Note: following conditional is leftover from Millframe, left here as a reminder of previously supported functionality
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

            Trace.WriteLine($"Content reduction task with ID '{NewDocumentTask.ID.ToString("N")}' successfully saved");

            return ReturnTaskInfo;
        }

        /// <summary>
        /// Manages the execution of a QDS task and monitors the completion of that task
        /// </summary>
        /// <param name="TInfo"></param>
        /// <returns></returns>
        private async Task RunQdsTask(QmsApi.TaskInfo TInfo)
        {
            // TODO make these configurable?
            TimeSpan MaxStartDelay = new TimeSpan(0, 5, 0);
            TimeSpan MaxElapsedRun = new TimeSpan(0, 5, 0);
            int PublisherPollingIntervalMs = 250;

            QmsApi.TaskStatus Status;

            IQMS QmsClient = QmsClientCreator.New(QmsUrl);

            // Get the task started, this generally requires more than one call to RunTaskAsync
            DateTime StartTime = DateTime.Now;
            do
            {
                if (DateTime.Now - StartTime > MaxStartDelay)
                {
                    throw new System.Exception($"Qlikview publisher failed to start task {TInfo.ID} before timeout");
                }

                await QmsClient.RunTaskAsync(TInfo.ID);
                Thread.Sleep(PublisherPollingIntervalMs);

                Status = await QmsClient.GetTaskStatusAsync(TInfo.ID, TaskStatusScope.All);
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

                Thread.Sleep(PublisherPollingIntervalMs);

                Status = await QmsClient.GetTaskStatusAsync(TInfo.ID, TaskStatusScope.All);
            } while (Status == null || Status.Extended == null || !DateTime.TryParse(Status.Extended.FinishedTime, out _));
            Trace.WriteLine($"In QvReductionRunner.RunQdsTask() task {TInfo.ID} finished running after {DateTime.Now - RunningStartTime}");

            if (Status.General.Status == TaskStatusValue.Failed)
            {
                throw new ApplicationException($"Qlikview server error while processing task {TInfo.ID}:{Environment.NewLine}{Status.Extended.LastLogMessages}");
            }
        }

        /// <summary>
        /// Deletes a Qlikview server task. 
        /// </summary>
        /// <param name="TInfo"></param>
        /// <returns></returns>
        private async Task<bool> DeleteQdsTask(QmsApi.TaskInfo TInfo)
        {
            IQMS QmsClient = QmsClientCreator.New(QmsUrl);
            QmsApi.TaskStatus Status = await QmsClient.GetTaskStatusAsync(TInfo.ID, TaskStatusScope.All);

            // null should indicate that the task doesn't exist
            bool Result = (Status == null) || await QmsClient.DeleteTaskAsync(TInfo.ID, TInfo.Type);

            return Result;
        }

    }
}

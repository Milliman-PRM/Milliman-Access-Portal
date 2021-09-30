/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: The reduction runner class for Qlikview content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using MapCommonLib;
using MapDbContextLib.Models;
using QlikviewLib.Qms;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;

namespace ContentPublishingLib.JobRunners
{
    public class QvReductionRunner : RunnerBase
    {
        private string QmsUrl = null;

        private IQMS _newQdsClient
        {
            get => QmsClientCreator.New(QmsUrl);
        }

        /// <summary>
        /// Constructor, sets up starting conditions that are associated with the system configuration rather than this specific task.
        /// </summary>
        public QvReductionRunner()
        {
            // Initialize members
            QmsUrl = Configuration.ApplicationConfiguration["QdsQmsApiUrl"];
            string configuredSourceDocsPath = Configuration.ApplicationConfiguration["Storage:QvSourceDocumentsPath"];

            Task initTask = Task.Run(async () =>
            {
                List<ServiceInfo> services = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, List<ServiceInfo>>(
                    async () => await _newQdsClient.GetServicesAsync(ServiceTypes.QlikViewDistributionService), 2, 250);
                QdsServiceInfo = services[0];

                // Qv can have 0 or more configured source document folders, need to find the right one. 
                List<DocumentFolder> docFolderArray = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, List<DocumentFolder>>(
                    async () => await _newQdsClient.GetSourceDocumentFoldersAsync(QdsServiceInfo.ID, DocumentFolderScope.All), 2, 200);
                foreach (DocumentFolder DocFolder in docFolderArray)
                {
                    // eliminate any trailing slash issue
                    if (Path.GetFullPath(configuredSourceDocsPath) == Path.GetFullPath(DocFolder.General.Path))
                    {
                        SourceDocFolder = DocFolder;
                        return;  // Returns from this Task, not the method
                    }
                }

                string serverSourceDocsList = string.Join(", ", docFolderArray.Select(f => Path.GetFullPath(f.General.Path)));
                throw new ApplicationException($"Configured QVP Source Document folder {Path.GetFullPath(configuredSourceDocsPath)} not found by Qlikview server, available choices are {serverSourceDocsList}");
            });
            while (!initTask.IsCompleted)
            {
                Thread.Sleep(10);
            }
            if (initTask.IsFaulted)
            {
                Log.Error(initTask.Exception, "Exception thrown during QvReductionRunner constructor");
            }
        }

        #region Member properties
        public ReductionJobDetail JobDetail { get; set; } = new ReductionJobDetail();

        private DocumentFolder SourceDocFolder { get; set; } = null;

        private string WorkingFolderRelative { get; set; } = string.Empty;

        private ServiceInfo QdsServiceInfo { get; set; } = null;

        private string MasterFileName { get { return Path.GetFileName(JobDetail.Request.MasterFilePath); } }

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

            object DetailObj;

            ReductionJobActionEnum[] SupportedJobActions = new ReductionJobActionEnum[] 
            {
                ReductionJobActionEnum.HierarchyOnly,
                ReductionJobActionEnum.HierarchyAndReduction,
                ReductionJobActionEnum.ReductionOnly,
            };

            DateTime ProcessingStartTime = DateTime.UtcNow;

            try
            {
                if (!SupportedJobActions.Contains(JobDetail.Request.JobAction))
                {
                    JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.BadRequest;
                    throw new ApplicationException($"QvReductionRunner.Execute() refusing to process job with unsupported requested action: {JobDetail.Request.JobAction.GetDisplayNameString()}");
                }
                else
                {
                    ValidateThisInstance();
                    _CancellationToken.ThrowIfCancellationRequested();

                    await PreTaskSetupAsync();
                    _CancellationToken.ThrowIfCancellationRequested();

                    #region Extract master content hierarchy
                    if (JobDetail.Request.JobAction != ReductionJobActionEnum.ReductionOnly)
                    {
                        JobDetail.Result.MasterContentHierarchy = await ExtractReductionHierarchy(MasterDocumentNode);

                        DetailObj = new
                        {
                            ReductionJobId = JobDetail.TaskId.ToString(),
                            JobAction = JobDetail.Request.JobAction,
                            Hierarchy = JobDetail.Result.MasterContentHierarchy,
                            ContentFile = "Master",
                        };
                        AuditLog.Log(AuditEventType.HierarchyExtractionSucceeded.ToEvent(DetailObj), null, null);
                    }
                    #endregion

                    _CancellationToken.ThrowIfCancellationRequested();

                    if (JobDetail.Request.JobAction != ReductionJobActionEnum.HierarchyOnly)
                    {
                        #region Create reduced content
                        await CreateReducedContent();

                        DetailObj = new {
                            ReductionJobId = JobDetail.TaskId.ToString(),
                            RequestedSelections = JobDetail.Request.SelectionCriteria,
                        };
                        AuditLog.Log(AuditEventType.ContentFileReductionSucceeded.ToEvent(DetailObj), null, null);
                        #endregion

                        _CancellationToken.ThrowIfCancellationRequested();

                        #region Extract reduced content hierarchy
                        JobDetail.Result.ReducedContentHierarchy = await ExtractReductionHierarchy(ReducedDocumentNode);

                        DetailObj = new {
                            ReductionJobId = JobDetail.TaskId.ToString(),
                            JobAction = JobDetail.Request.JobAction,
                            Hierarchy = JobDetail.Result.ReducedContentHierarchy,
                            ContentFile = "Reduced",
                        };
                        AuditLog.Log(AuditEventType.HierarchyExtractionSucceeded.ToEvent(DetailObj), null, null);
                        #endregion

                        _CancellationToken.ThrowIfCancellationRequested();

                        DistributeReducedContent();
                    }

                    JobDetail.Status = ReductionJobDetail.JobStatusEnum.Success;
                    JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.Success;
                }
            }
            catch (OperationCanceledException e)
            {
                JobDetail.Status = ReductionJobDetail.JobStatusEnum.Canceled;
                JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.Canceled;
                Log.Warning(e, $"Operation Cancelled in QvReductionRunner");
                JobDetail.Result.StatusMessage = GlobalFunctions.LoggableExceptionString(e, $"Exception in QvReductionRunner", true, true);
                AuditLog.Log(AuditEventType.ContentReductionTaskCanceled.ToEvent(new { ReductionTaskId = JobDetail.TaskId }), null, null);
            }
            catch (ApplicationException e)
            {
                // JobDetail.Result.OutcomeReason should be set where the problem occurs before throwing to here
                // Any security related audit logs are generated at the time ApplicationException is thrown, where appropriate.  Don't repeat that here. 

                List<ReductionJobDetail.JobOutcomeReason> WarningStatusReasons = new List<ReductionJobDetail.JobOutcomeReason>
                {
                    ReductionJobDetail.JobOutcomeReason.NoSelectedFieldValueExistsInNewContent,
                    ReductionJobDetail.JobOutcomeReason.NoSelectedFieldValues,
                };

                // JobDetail.Result.OutcomeReason is expected to be set where the exception is thrown
                if (WarningStatusReasons.Contains(JobDetail.Result.OutcomeReason))
                {
                    JobDetail.Status = ReductionJobDetail.JobStatusEnum.Warning;
                }
                else
                {
                    JobDetail.Status = ReductionJobDetail.JobStatusEnum.Error;
                }

                Log.Warning(e, $"ApplicationException in QvReductionRunner");
                JobDetail.Result.StatusMessage = GlobalFunctions.LoggableExceptionString(e, $"Exception in QvReductionRunner", true, true);
            }
            catch (System.Exception e)
            {
                JobDetail.Status = ReductionJobDetail.JobStatusEnum.Error;
                Log.Error(e, "System.Exception in QvReductionRunner");
                JobDetail.Result.StatusMessage = GlobalFunctions.LoggableExceptionString(e, $"Exception in QvReductionRunner", true, true);
                DetailObj = new
                {
                    ReductionJobId = JobDetail.TaskId.ToString(),
                    ExceptionMessage = GlobalFunctions.LoggableExceptionString(e),
                };
                AuditLog.Log(AuditEventType.ContentFileReductionFailed.ToEvent(DetailObj), null, null);
                if (JobDetail.Result.OutcomeReason == ReductionJobDetail.JobOutcomeReason.Unspecified)
                {
                    JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.UnspecifiedError;
                }
            }
            finally
            {
                // Don't touch JobDetail.Result.Status or JobDetail.Result.OutcomeReason in the finally block. This value should always be set before we get here. 
                JobDetail.Result.ProcessingDuration = DateTime.UtcNow - ProcessingStartTime;
                try
                {
                    bool cleanupResult = Cleanup();
                }
                catch (System.Exception e)  // fail safe in case any exception gets to this point
                {
                    Log.Error(e, $"In QvReductionRunner.Execute(), Cleanup() failed");
                }
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

            else if (JobDetail.Request.JobAction == ReductionJobActionEnum.ReductionOnly
                  && JobDetail.Result.MasterContentHierarchy == null)
            {
                Msg = $"ReductionOnly processing was requested without a provided master hierarchy";
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

                object DetailObj = new {
                    ReductionJobId = JobDetail.TaskId.ToString(),
                    Error = Msg,
                };
                AuditLog.Log(AuditEventType.ReductionValidationFailed.ToEvent(DetailObj), null, null);

                Msg = $"Error in {Method.ReflectedType.Name}.{Method.Name}: {Msg}";

                JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.BadRequest;
                throw new System.ApplicationException(Msg);
            }
        }

        /// <summary>
        /// Sets up the starting conditions that are unique to this specific task
        /// </summary>
        private async Task PreTaskSetupAsync()
        {
            WorkingFolderRelative = JobDetail.TaskId.ToString();  // Folder is named for the reduction task guid
            string WorkingFolderAbsolute = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);
            string MasterFileDestinationPath = Path.Combine(WorkingFolderAbsolute, MasterFileName);

            try
            {
                // remove pre-existing task folder of same name, normally won't exist but maybe in development environment
                if (Directory.Exists(WorkingFolderAbsolute) && !string.IsNullOrWhiteSpace(WorkingFolderRelative))
                {
                    Log.Warning($"QvReductionRunner.PreTaskSetupAsync(), reduction Task working directory {WorkingFolderAbsolute} already exists.  Deleting");
                    FileSystemUtil.DeleteDirectoryWithRetry(WorkingFolderAbsolute, true);
                }

                // Make sure the requested master content file exists
                if (!File.Exists(JobDetail.Request.MasterFilePath))
                {
                    JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.BadRequest;
                    throw new ApplicationException($"Master file {JobDetail.Request.MasterFilePath} does not exist");
                }

                Directory.CreateDirectory(WorkingFolderAbsolute);
                Log.Information($"QvReductionRunner.PreTaskSetupAsync(), reduction Task working directory {WorkingFolderAbsolute} created");
                File.Copy(JobDetail.Request.MasterFilePath, MasterFileDestinationPath);

                // Set this.MasterDocumentNode, which is used elsewhere in this class
                MasterDocumentNode = await GetSourceDocumentNode(MasterFileName, WorkingFolderRelative);
            }
            catch (System.Exception e)
            {
                Log.Information(e, $"QvReductionRunner.PreTaskSetupAsync() failed to create folder {WorkingFolderAbsolute} or copy master file {JobDetail.Request.MasterFilePath} to {MasterFileDestinationPath}");
                throw;
            }

            if (MasterDocumentNode == null)
            {
                JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.BadRequest;
                throw new ApplicationException($"QvReductionRunner.PreTaskSetupAsync() failed to obtain DocumentNode object from Qlikview Publisher for master content file {MasterFileName} in relative folder {WorkingFolderRelative}");
            }
        }

        /// <summary>
        /// Extracts the reduction fields and corresponding values of a Qlikview content item
        /// </summary>
        private async Task<ExtractedHierarchy> ExtractReductionHierarchy(DocumentNode DocumentNodeArg)
        {
            ExtractedHierarchy ResultHierarchy = new ExtractedHierarchy();

            // Create ancillary script
            string AncillaryScriptFilePath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, "ancillary_script.txt");
            File.WriteAllText(AncillaryScriptFilePath, "LET DataExtraction=true(); LET MAP_Reduction=true();");

            // Create Qlikview publisher (QDS) task
            DocumentTask HierarchyTask = CreateHierarchyExtractionQdsTask(DocumentNodeArg);

            // Run Qlikview publisher (QDS) task
            try
            {
                string AbsoluteDocPath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, DocumentNodeArg.Name);
                int FileSizeHectoMillionBytes = (int)(new FileInfo(AbsoluteDocPath).Length / 1E8 );
                await RunQdsTaskAsync(HierarchyTask, Math.Max(FileSizeHectoMillionBytes, 5));  // Allow 1 minute per 1E8 Bytes, at least 5 minutes
            }
            finally
            {
                // Clean up
                try
                {
                    FileSystemUtil.DeleteFileWithRetry(AncillaryScriptFilePath);
                }
                catch { }
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

                    // Clean up any values that have surrounding double quotes, usually due to special character(s)
                    for (int valCounter = 0; valCounter < NewField.FieldValues.Count; valCounter++)
                    {
                        string valFromFile = NewField.FieldValues[valCounter];
                        if (valFromFile.StartsWith("\"") && valFromFile.EndsWith("\""))
                        {
                            NewField.FieldValues[valCounter] = valFromFile
                                .Substring(1, valFromFile.Length - 2)  // remove enclosing quotes
                                .Replace("\"\"", "\"");  // replace each pair of (escaped) '"' with a single '"'
                        }
                    }

                    FileSystemUtil.DeleteFileWithRetry(ValuesFileName);

                    ResultHierarchy.Fields.Add(NewField);
                }
            }
            catch (System.Exception e)
            {
                JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.HierarchyExtractionFailed;
                string errMsg = $"Failed to extract content reduction hierarchy, error converting file {ReductionSchemeFilePath} to json output{Environment.NewLine}{e.Message}";
                Log.Error(e, errMsg);

                object DetailObj = new {
                    ReductionJobId = JobDetail.TaskId.ToString(),
                    ProblemDetail = errMsg,
                };
                AuditLog.Log(AuditEventType.HierarchyExtractionFailed.ToEvent(DetailObj), null, null);

                throw new ApplicationException(errMsg);
            }
            finally
            {
                try
                {
                    FileSystemUtil.DeleteFileWithRetry(ReductionSchemeFilePath);
                }
                catch { }
            }
            #endregion

            foreach (string LogFile in Directory.EnumerateFiles(Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative), DocumentNodeArg.Name + "*.log"))
            {
                try
                {
                    FileSystemUtil.DeleteFileWithRetry(LogFile);
                }
                catch (System.Exception e)
                {
                    Log.Error(e, $"Failed to delete Qlikview task log file {LogFile}");
                    throw;
                }
            }

            Log.Information($"QvReductionRunner.ExtractReductionHierarchy(), reduction Task {JobDetail.TaskId} completed ExtractReductionHierarchy");

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
                    object DetailObj = new {
                        ReductionJobId = JobDetail.TaskId.ToString(),
                        Error = Msg,
                    };

                    JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.SelectionForInvalidFieldName;
                    AuditLog.Log(AuditEventType.ContentFileReductionFailed.ToEvent(DetailObj), null, null);
                    throw new ApplicationException(Msg);
                }
            }

            // Validate that there is at least one selected value that exists in the hierarchy. 
            if (!JobDetail.Request.SelectionCriteria.Any(s => s.Selected &&
                                                              JobDetail.Result.MasterContentHierarchy.Fields.Any(f => f.FieldName == s.FieldName && f.FieldValues.Contains(s.FieldValue))))
            {
                string Msg = $"None of the {JobDetail.Request.SelectionCriteria.Where(s => s.Selected).Count()} specified selections exist in the master content hierarchy";
                object DetailObj = new {
                    ReductionJobId = JobDetail.TaskId.ToString(),
                    RequestesSelections = JobDetail.Request.SelectionCriteria,
                    Error = Msg,
                };

                AuditLog.Log(AuditEventType.ContentFileReductionFailed.ToEvent(DetailObj), null, null);

                JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.NoSelectedFieldValueExistsInNewContent;

                throw new ApplicationException(Msg);
            }

            // Create Qlikview publisher (QDS) task
            DocumentTask ReductionTask = CreateReductionQdsTask(JobDetail.Request.SelectionCriteria);

            // Run Qlikview publisher (QDS) task
            await RunQdsTaskAsync(ReductionTask);

            ReducedDocumentNode = await GetSourceDocumentNode(JobDetail.Request.RequestedOutputFileName, WorkingFolderRelative);

            if (ReducedDocumentNode == null)
            {
                JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.NoReducedFileCreated;
                string Msg = $"Failed to get DocumentNode for file {JobDetail.Request.RequestedOutputFileName} in folder {SourceDocFolder.General.Path}\\{WorkingFolderRelative}";
                throw new ApplicationException(Msg);
            }

            Log.Information($"QvReductionRunner.CreateReducedContent() reduction Task {JobDetail.TaskId} completed CreateReducedContent");
        }

        /// <summary>
        /// Makes the outcome of the content reduction operation accessible to the main application
        /// </summary>
        private void DistributeReducedContent()
        {
            string ApplicationDataExchangeFolder = Path.GetDirectoryName(JobDetail.Request.MasterFilePath);
            string WorkingFolderAbsolute = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);

            string ReducedFile = Directory.GetFiles(WorkingFolderAbsolute, JobDetail.Request.RequestedOutputFileName).Single();
            string CopyDestinationPath = Path.Combine(ApplicationDataExchangeFolder, Path.GetFileName(ReducedFile));

            FileSystemUtil.CopyFileWithRetry(ReducedFile, CopyDestinationPath, true);
            JobDetail.Result.ReducedContentFileChecksum = GlobalFunctions.GetFileChecksum(CopyDestinationPath).checksum;
            JobDetail.Result.ReducedContentFilePath = CopyDestinationPath;

            Log.Information($"QvReductionRunner.DistributeReducedContent() reduction Task {JobDetail.TaskId} completed DistributeReducedContent");
        }

        /// <summary>
        /// Remove all temporary artifacts of the entire process
        /// </summary>
        private bool Cleanup()
        {
            if (SourceDocFolder is null)
            {
                Log.Warning($"QvReductionRunner.Cleanup(), unable to run because SourceDocFolder is null");
                return false;
            }
            else
            {
                string WorkingFolderAbsolute = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);
                if (!string.IsNullOrWhiteSpace(WorkingFolderRelative) && Directory.Exists(WorkingFolderAbsolute))
                {
                    try
                    {
                        FileSystemUtil.DeleteDirectoryWithRetry(WorkingFolderAbsolute, true);
                    }
                    catch (System.Exception e)  // Do not let this throw upward
                    {
                        // It's an error, but the reduction task has completed by now so just log this and continue.
                        Log.Error(e, $"QvReductionRunner.Cleanup(), failed to delete temporary reduction directory {WorkingFolderAbsolute}, continuing");
                    }
                }

                Log.Information($"QvReductionRunner.Cleanup(), reduction Task {JobDetail.TaskId} completed Cleanup");
                return true;
            }
        }

        /// <summary>
        /// Returns Qv DocumentNode object corresponding to the requested file 
        /// </summary>
        /// <param name="RequestedFileName"></param>
        /// <param name="RequestedRelativeFolder">Path relative to the selected source documents folder.</param>
        /// <returns>null if not found</returns>
        private async Task<DocumentNode> GetSourceDocumentNode(string RequestedFileName, string RequestedRelativeFolder)
        {
            DocumentNode DocNode = null;

            DateTime Start = DateTime.Now;
            while (DocNode == null && (DateTime.Now - Start) < new TimeSpan(0, 1, 10))  // QV server seems to poll for files every minute
            {
                Thread.Sleep(500);
                List<DocumentNode> allDocNodes = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, List<DocumentNode>>(
                    async () => await _newQdsClient.GetSourceDocumentNodesAsync(QdsServiceInfo.ID, SourceDocFolder.ID, RequestedRelativeFolder), 2, 250);
                DocNode = allDocNodes.SingleOrDefault(dn => dn.FolderID == SourceDocFolder.ID
                                                         && dn.Name == RequestedFileName
                                                         && dn.RelativePath == RequestedRelativeFolder);
            }

            if (DocNode == null)
            {
                // Don't throw here, caller can decide what to do
                Log.Error($"QvReductionRunner.GetSourceDocumentNode() did not find SourceDocument '{RequestedFileName}' in subfolder {RequestedRelativeFolder} of source documents folder {SourceDocFolder.General.Path}");
            }

            return DocNode;
        }

        /// <summary>
        /// Creates and stores a Qlikview publisher task to extract the selection hierarchy info for a QV document
        /// </summary>
        /// <param name="DocNodeArg"></param>
        /// <returns></returns>
        private DocumentTask CreateHierarchyExtractionQdsTask(DocumentNode DocNodeArg)
        {
            string TaskDateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Create new task object
            DocumentTask NewDocumentTask = new DocumentTask
            {
                ID = Guid.NewGuid(),
                QDSID = QdsServiceInfo.ID,
                Document = DocNodeArg,
            };

            #region General Tab
            NewDocumentTask.Scope |= DocumentTaskScope.General;

            NewDocumentTask.General = new DocumentTask.TaskGeneral
            {
                Enabled = true,
                TaskName = $"Hierarchy task {NewDocumentTask.ID} for job {JobDetail.TaskId} at {TaskDateTimeStamp}",
                TaskDescription = $"Automatically generated by ReductionService at {TaskDateTimeStamp}",
            };
            #endregion

            #region Reload Tab
            NewDocumentTask.Scope |= DocumentTaskScope.Reload;

            NewDocumentTask.Reload = new DocumentTask.TaskReload
            {
                Mode = TaskReloadMode.Partial,
            };
            #endregion

            #region Reduce Tab
            NewDocumentTask.Scope |= DocumentTaskScope.Reduce;

            NewDocumentTask.Reduce = new DocumentTask.TaskReduce
            {
                DocumentNameTemplate = string.Empty,
                Static = new DocumentTask.TaskReduce.TaskReduceStatic
                {
                    Reductions = new List<TaskReduction> { new TaskReduction { Type = new TaskReductionType() } },
                },
                Dynamic = new DocumentTask.TaskReduce.TaskReduceDynamic { Type = new TaskReductionType() },
            };
            #endregion

            #region Trigger Tab
            NewDocumentTask.Scope |= DocumentTaskScope.Triggering;

            NewDocumentTask.Triggering = new DocumentTask.TaskTriggering
            {
                ExecutionAttempts = 1,
                ExecutionTimeout = 1440,
                TaskDependencies = new List<TaskInfo> { },
                Triggers = new List<Trigger> {
                    new ScheduleTrigger
                    {
                        Enabled = true,
                        Type = TaskTriggerType.OnceTrigger,
                        StartAt = DateTime.Now,
                    }
                }
            };
            #endregion

            return NewDocumentTask;
        }

        /// <summary>
        /// Creates and stores a Qlikview publisher task to produce a reduced version of a master QV document
        /// </summary>
        /// <param name="Selections"></param>
        /// <returns></returns>
        private DocumentTask CreateReductionQdsTask(IEnumerable<FieldValueSelection> Selections)
        {
            string TaskDateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Create new task object
            DocumentTask NewDocumentTask = new DocumentTask
            {
                ID = Guid.NewGuid(),
                QDSID = QdsServiceInfo.ID,
                Document = MasterDocumentNode
            };

            #region general
            NewDocumentTask.Scope |= DocumentTaskScope.General;

            NewDocumentTask.General = new DocumentTask.TaskGeneral
            {
                Enabled = true,
                TaskName = $"Reload and partial reduction task {NewDocumentTask.ID} for job {NewDocumentTask.ID} at {TaskDateTimeStamp}",
                TaskDescription = $"Automatically generated by ReductionService at {TaskDateTimeStamp}",
            };
            #endregion

            #region reduce
            NewDocumentTask.Scope |= DocumentTaskScope.Reduce;

            NewDocumentTask.Reduce = new DocumentTask.TaskReduce
            {
                DocumentNameTemplate = Path.GetFileNameWithoutExtension(JobDetail.Request.RequestedOutputFileName),
                Static = new DocumentTask.TaskReduce.TaskReduceStatic
                {
                    Reductions = new List<TaskReduction>(),
                }
            };

            foreach (FieldValueSelection FieldVal in Selections.Where(v => v.Selected))
            {
                NewDocumentTask.Reduce.Static.Reductions.Add(new TaskReduction
                {
                    Type = TaskReductionType.ByField,
                    Field = new TaskReduction.TaskReductionField
                    {
                        Name = FieldVal.FieldName,
                        Value = FieldVal.FieldValue,
                        IsNumeric = double.TryParse(FieldVal.FieldValue, out _),
                    }
                });
            }

            NewDocumentTask.Reduce.Dynamic = new DocumentTask.TaskReduce.TaskReduceDynamic();
            #endregion

            #region distribute
            NewDocumentTask.Scope |= DocumentTaskScope.Distribute;

            NewDocumentTask.Distribute = new DocumentTask.TaskDistribute
            {
                Output = new DocumentTask.TaskDistribute.TaskDistributeOutput { Type = TaskDistributionOutputType.QlikViewDocument },
                Static = new DocumentTask.TaskDistribute.TaskDistributeStatic
                {
                    DistributionEntries = new List<TaskDistributionEntry> { new TaskDistributionEntry 
                    {
                        Recipients = new List<DirectoryServiceObject>{ new DirectoryServiceObject{ Type = DirectoryServiceObjectType.Authenticated } },
                        Destination = new TaskDistributionDestination
                        {
                            Folder = new TaskDistributionDestination.TaskDistributionDestinationFolder{ Name = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative) },
                            Type = TaskDistributionDestinationType.Folder,
                        },
                    }},
                },
                Notification = new DocumentTask.TaskDistribute.TaskDistributeNotification { SendNotificationEmail = false },
                Dynamic = new DocumentTask.TaskDistribute.TaskDistributeDynamic
                {
                    IdentityType = UserIdentityValueType.DisplayName,
                    Destinations = new List<TaskDistributionDestination>(),
                },
            };
            #endregion

            #region reload
            NewDocumentTask.Scope |= DocumentTaskScope.Reload;

            NewDocumentTask.Reload = new DocumentTask.TaskReload
            {
                // Note: following conditional is leftover from Millframe, left here as a reminder of previously supported functionality
                //if (drop_required)
                //    task_reduction.Reload.Mode = TaskReloadMode.Partial; //this will result in tables being dropped as defined by the drop table processor
                //else
                Mode = TaskReloadMode.None,
            };
            #endregion

            #region trigger
            NewDocumentTask.Triggering = new DocumentTask.TaskTriggering
            {
                ExecutionAttempts = 1,
                ExecutionTimeout = 1440,
                TaskDependencies = new List<TaskInfo> { },
                Triggers = new List<Trigger>
                {
                    new ScheduleTrigger
                    {
                        Enabled = true,
                        Type = TaskTriggerType.OnceTrigger,
                        StartAt = DateTime.Now.AddSeconds(5.0), //don't schedule for now, it ignores if in past
                    }
                },
            };
            #endregion

            return NewDocumentTask;
        }

        /// <summary>
        /// Manages the execution of a QDS task and monitors the completion of that task
        /// </summary>
        /// <param name="TInfo"></param>
        /// <returns></returns>
        private async Task RunQdsTaskAsync(DocumentTask DocTask, int? timeoutMinutes = null)
        {
            var defaultTimeout = int.Parse(Configuration.ApplicationConfiguration["DefaultQdsTaskTimeoutMinutes"]);
            TimeSpan MaxStartDelay = new TimeSpan(0, 0, 5, 0);
            TimeSpan MaxElapsedRun = new TimeSpan(0, 0, timeoutMinutes ?? defaultTimeout, 0);
            TimeSpan TaskStartPollingInterval = new TimeSpan(0, 0, 0, 10);
            TimeSpan PublisherPollingInterval = new TimeSpan(0, 0, 0, 1);

            QlikviewLib.Qms.TaskStatus status = default;

            // Save the task to Qlikview server
            DateTime SaveStartTime = DateTime.Now;
            try
            {
                await StaticUtil.DoRetryAsyncOperation<AggregateException>(async () => await _newQdsClient.SaveDocumentTaskAsync(DocTask), 2, 250);
            }
            catch (System.Exception ex)
            {
                throw new ApplicationException("QmsClient.SaveDocumentTaskAsync exception", ex);
            }

            TaskInfo TInfo = default;
            try
            {
                TInfo = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, TaskInfo>(
                    async () => await _newQdsClient.FindTaskAsync(QdsServiceInfo.ID, TaskType.DocumentTask, DocTask.General.TaskName), 2, 250);
            }
            catch (System.Exception ex)
            {
                throw new ApplicationException("After saving task, QmsClient.FindTaskAsync exception", ex);
            }
            Guid TaskIdGuid = TInfo.ID;
            Log.Information($"QvReductionRunner.RunQdsTaskAsync() QDS task {TaskIdGuid} saved, and task info retrieved, after {DateTime.Now - SaveStartTime}");

            try
            {
                DateTime RunStartTime = DateTime.Now;
                int pollTaskStartRetryCount = 3;
                // Get the task started, this generally requires more than one call to RunTaskAsync
                do
                {
                    if (DateTime.Now - RunStartTime > MaxStartDelay)
                    {
                        JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.ReductionProcessingTimeout;
                        throw new System.Exception($"Qlikview publisher failed to start task {TaskIdGuid} before timeout");
                    }

                    try
                    {
                        await StaticUtil.DoRetryAsyncOperation<AggregateException>(async () => await _newQdsClient.RunTaskAsync(TaskIdGuid), 2, 250);
                    }
                    catch (System.Exception ex)
                    {
                        throw new ApplicationException("QmsClient.RunTaskAsync exception", ex);
                    }

                    Thread.Sleep(TaskStartPollingInterval);

                    try
                    {
                        status = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, QlikviewLib.Qms.TaskStatus>(
                            async () => await _newQdsClient.GetTaskStatusAsync(TaskIdGuid, TaskStatusScope.All), 2, 250);
                    }
                    catch (System.Exception ex)
                    {
                        if (pollTaskStartRetryCount-- > 0)
                        {
                            Log.Information(ex, "QvReductionRunner.RunQdsTaskAsync() Retrying after exception while polling for task status after RunTaskAsync");
                            continue;
                        }
                        throw new ApplicationException("Exceeded maximum retries for QmsClient.GetTaskStatusAsync while trying to start task", ex);
                    }
                } while (status == null || status.Extended == null || !(DateTime.TryParse(status.Extended.StartTime, out _) || DateTime.TryParse(status.Extended.FinishedTime, out _)));
                Log.Information($"QvReductionRunner.RunQdsTaskAsync() QDS task {TaskIdGuid} started running after {DateTime.Now - RunStartTime}");

                // Wait for started task to finish
                DateTime RunningStartTime = DateTime.Now;
                int pollTaskFinishRetryCount = 3;
                do
                {
                    if (DateTime.Now - RunningStartTime > MaxElapsedRun)
                    {
                        JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.ReductionProcessingTimeout;
                        throw new System.Exception($"In QvReductionRunner.RunQdsTask() QDS task {TaskIdGuid} failed to finish before timeout ({MaxElapsedRun})");
                    }

                    Thread.Sleep(PublisherPollingInterval);

                    try
                    {
                        status = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, QlikviewLib.Qms.TaskStatus>(
                            async () => await _newQdsClient.GetTaskStatusAsync(TaskIdGuid, TaskStatusScope.All), 2, 250);
                    }
                    catch (System.Exception ex)
                    {
                        if (pollTaskFinishRetryCount-- > 0)
                        {
                            Log.Information(ex, "QvReductionRunner.RunQdsTaskAsync() Retrying after exception while polling for task status while task is running");
                            continue;
                        }
                        throw new ApplicationException("Exceeded maximum retries for QmsClient.GetTaskStatusAsync while waiting for task to finish", ex);
                    }
                } while (status == null || status.Extended == null || !DateTime.TryParse(status.Extended.FinishedTime, out _));
                Log.Information($"QvReductionRunner.RunQdsTaskAsync() QDS task {TaskIdGuid} finished running after {DateTime.Now - RunningStartTime}");

                switch (status.General.Status)
                {
                    case TaskStatusValue.Warning:
                        string ExpectedReducedFilePath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, JobDetail.Request.RequestedOutputFileName);
                        if (DocTask.Reload.Mode == TaskReloadMode.None // reduction task, not hierarchy extraction
                            && !File.Exists(ExpectedReducedFilePath))
                        {
                            JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.NoReducedFileCreated;
                        }
                        // else if (other outcomes) as we discover them go here
                        else
                        {
                            JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.UnspecifiedError;
                        }
                        throw new ApplicationException($"QDS status {status.General.Status.GetDisplayNameString()} after task {TaskIdGuid}:{Environment.NewLine}{status.Extended.LastLogMessages}");

                    case TaskStatusValue.Failed:
                        JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.UnspecifiedError;
                        throw new ApplicationException($"QDS status {status.General.Status.GetDisplayNameString()} after task {TaskIdGuid}:{Environment.NewLine}{status.Extended.LastLogMessages}");
                }
            }
            finally
            {
                // Clean up
                try
                {
                    status = await StaticUtil.DoRetryAsyncOperationWithReturn<AggregateException, QlikviewLib.Qms.TaskStatus>(
                        async () => await _newQdsClient.GetTaskStatusAsync(TaskIdGuid, TaskStatusScope.All), 2, 250);
                }
                catch (System.Exception ex)
                {
                    throw new ApplicationException("QmsClient.GetTaskStatusAsync (in final cleanup) exception", ex);
                }

                // null would indicate that the task doesn't exist
                if (status != null)
                {
                    try
                    {
                        await StaticUtil.DoRetryAsyncOperation<AggregateException>(
                            async () => await _newQdsClient.DeleteTaskAsync(TaskIdGuid, TInfo.Type), 2, 250);
                        Log.Information($"QvReductionRunner.RunQdsTaskAsync() QDS task {TaskIdGuid} deleted");
                    }
                    catch (System.Exception ex)
                    {
                        throw new ApplicationException($"In QvReductionRunner.RunQdsTask() QmsClient.DeleteTaskAsync({TaskIdGuid}) exception", ex);
                    }
                }
            }
        }

        private void LogAcl(string path)
        {
            FileSecurity acl = new FileInfo(path).GetAccessControl();
            AuthorizationRuleCollection securityIdentifierRules = acl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
            AuthorizationRuleCollection ntAccountRules = acl.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            Log.Information($"  SecurityIdentifier ACL rules:");
            foreach (FileSystemAccessRule rule in securityIdentifierRules)
            {
                Log.Information($"    {rule.AccessControlType} {rule.FileSystemRights},   IsInherited: {rule.IsInherited}");
            }
            Log.Information($"  NTAccount ACL rules:");
            foreach (FileSystemAccessRule rule in ntAccountRules)
            {
                Log.Information($"    {rule.AccessControlType} {rule.FileSystemRights},   IsInherited: {rule.IsInherited}");
            }
        }

    }
}

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

            Task initTask = Task.Run(async () =>
            {
                ServiceInfo[] services = await _newQdsClient.GetServicesAsync(ServiceTypes.QlikViewDistributionService);
                QdsServiceInfo = services[0];

                // Qv can have 0 or more configured source document folders, need to find the right one. 
                var serverDocFolders = await _newQdsClient.GetSourceDocumentFoldersAsync(QdsServiceInfo.ID, DocumentFolderScope.All);
                foreach (DocumentFolder DocFolder in serverDocFolders)
                {
                    // eliminate any trailing slash issue
                    if (Path.GetFullPath(Configuration.ApplicationConfiguration["Storage:QvSourceDocumentsPath"]) == Path.GetFullPath(DocFolder.General.Path))
                    {
                        SourceDocFolder = DocFolder;
                        return;  // Returns from this Task, not the method
                    }
                }

                throw new ApplicationException($"Qlikview Source Document folder {Configuration.ApplicationConfiguration["Storage:QvSourceDocumentsPath"]} not found by Qlikview server");
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
                    throw new ApplicationException($"QvReductionRunner.Execute() refusing to process job with unsupported requested action: {JobDetail.Request.JobAction.ToString()}");
                }
                else
                {
                    ValidateThisInstance();
                    _CancellationToken.ThrowIfCancellationRequested();

                    await PreTaskSetup();
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
                        AuditLog.Log(AuditEventType.HierarchyExtractionSucceeded.ToEvent(DetailObj));
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
                        AuditLog.Log(AuditEventType.ContentFileReductionSucceeded.ToEvent(DetailObj));
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
                        AuditLog.Log(AuditEventType.HierarchyExtractionSucceeded.ToEvent(DetailObj));
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
                AuditLog.Log(AuditEventType.ContentReductionTaskCanceled.ToEvent(new { ReductionTaskId = JobDetail.TaskId }));
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
                AuditLog.Log(AuditEventType.ContentFileReductionFailed.ToEvent(DetailObj));
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
                    Cleanup();
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
                AuditLog.Log(AuditEventType.ReductionValidationFailed.ToEvent(DetailObj));

                Msg = $"Error in {Method.ReflectedType.Name}.{Method.Name}: {Msg}";

                JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.BadRequest;
                throw new System.ApplicationException(Msg);
            }
        }

        /// <summary>
        /// Sets up the starting conditions that are unique to this specific task
        /// </summary>
        private async Task<bool> PreTaskSetup()
        {
            WorkingFolderRelative = JobDetail.TaskId.ToString();  // Folder is named for the reduction task guid
            string WorkingFolderAbsolute = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);
            string MasterFileDestinationPath = Path.Combine(WorkingFolderAbsolute, MasterFileName);

            try
            {
                // remove pre-existing task folder of same name, normally won't exist but maybe in development environment
                if (Directory.Exists(WorkingFolderAbsolute) && !string.IsNullOrWhiteSpace(WorkingFolderRelative))
                {
                    FileSystemUtil.DeleteDirectoryWithRetry(WorkingFolderAbsolute);
                }

                // Make sure the requested master content file exists
                if (!File.Exists(JobDetail.Request.MasterFilePath))
                {
                    JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.BadRequest;
                    throw new ApplicationException($"Master file {JobDetail.Request.MasterFilePath} does not exist");
                }

                Directory.CreateDirectory(WorkingFolderAbsolute);
                File.Copy(JobDetail.Request.MasterFilePath, MasterFileDestinationPath);

                // Set this.MasterDocumentNode, which is used elsewhere in this class
                MasterDocumentNode = await GetSourceDocumentNode(MasterFileName, WorkingFolderRelative);
            }
            catch (System.Exception e)
            {
                Log.Information(e, $"QvReductionRunner.PreTaskSetup() failed to create folder {WorkingFolderAbsolute} or copy master file {JobDetail.Request.MasterFilePath} to {MasterFileDestinationPath}");
                throw;
            }

            if (MasterDocumentNode == null)
            {
                JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.BadRequest;
                throw new ApplicationException($"Failed to obtain DocumentNode object from Qlikview Publisher for master content file {MasterFileName} in relative folder {WorkingFolderRelative}");
            }
            return true;
        }

        /// <summary>
        /// Extracts the reduction fields and corresponding values of a Qlikview content item
        /// </summary>
        private async Task<ExtractedHierarchy> ExtractReductionHierarchy(DocumentNode DocumentNodeArg)
        {
            /* Delete this log */
            Log.Information($"At start of ExtractReductionHierarchy, folder {Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative)} exists ? {Directory.Exists(Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative))}");
            ExtractedHierarchy ResultHierarchy = new ExtractedHierarchy();

            // Create ancillary script
            string AncillaryScriptFilePath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, "ancillary_script.txt");
            File.WriteAllText(AncillaryScriptFilePath, "LET DataExtraction=true(); LET MAP_Reduction=true();");
            /* Delete this log */
            Log.Information($"After writing AncillaryScriptFile, folder {Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative)} exists ? {Directory.Exists(Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative))}");

            // Create Qlikview publisher (QDS) task
            DocumentTask HierarchyTask = CreateHierarchyExtractionQdsTask(DocumentNodeArg);

            // Run Qlikview publisher (QDS) task
            try
            {
                string AbsoluteDocPath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, DocumentNodeArg.Name);
                int FileSizeHectoMillionBytes = (int)(new FileInfo(AbsoluteDocPath).Length / 1E8 );
                await RunQdsTask(HierarchyTask, Math.Max(FileSizeHectoMillionBytes, 5));  // Allow 1 minute per 1E8 Bytes, at least 5 minutes
            }
            finally
            {
                // Clean up
                try
                {
                    /* Delete this log */
                    Log.Information($"Before deleting AncillaryScriptFilePath, folder {Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative)} exists ? {Directory.Exists(Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative))}");
                    FileSystemUtil.DeleteFileWithRetry(AncillaryScriptFilePath);
                    /* Delete this log */
                    Log.Information($"After deleting AncillaryScriptFilePath, folder {Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative)} exists ? {Directory.Exists(Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative))}");
                }
                catch { }
            }

            #region Build hierarchy json output
            string ReductionSchemeFilePath = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative, "reduction.scheme.csv");

            try
            {
                /* Delete this log */
                Log.Information($"Before reading reduction.scheme.csv, folder {Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative)} exists ? {Directory.Exists(Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative))}");
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

                #region temporary diagnostic code
                string share = Path.GetPathRoot(ReductionSchemeFilePath);
                string folder = Path.GetDirectoryName(ReductionSchemeFilePath);
                string parent = Path.GetDirectoryName(folder);

                Log.Information($"Checking the share <{share}>:");
                if (Directory.Exists(share))
                {
                    LogAcl(share);
                }
                else
                {
                    Log.Information($"Share <{share}> not found");
                }

                Log.Information($"Checking parent folder <{parent}>:");
                if (Directory.Exists(parent))
                {
                    LogAcl(parent);
                }
                else
                {
                    Log.Information($"  Parent folder <{parent}> not found");
                }

                Log.Information($"Checking target folder <{folder}>:");
                if (Directory.Exists(folder))
                {
                    LogAcl(folder);

                    foreach (var entry in Directory.EnumerateFileSystemEntries(folder))
                    {
                        Log.Error($"  Target Folder contains {entry}");
                    }
                }
                else
                {
                    Log.Information($"  Target folder <{folder}> not found");
                }
                #endregion

                object DetailObj = new {
                    ReductionJobId = JobDetail.TaskId.ToString(),
                    ProblemDetail = errMsg,
                };
                AuditLog.Log(AuditEventType.HierarchyExtractionFailed.ToEvent(DetailObj));

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

            Log.Information($"Task {JobDetail.TaskId.ToString()} completed ExtractReductionHierarchy");

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
                    AuditLog.Log(AuditEventType.ContentFileReductionFailed.ToEvent(DetailObj));
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

                AuditLog.Log(AuditEventType.ContentFileReductionFailed.ToEvent(DetailObj));

                JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.NoSelectedFieldValueExistsInNewContent;

                throw new ApplicationException(Msg);
            }

            // Create Qlikview publisher (QDS) task
            DocumentTask ReductionTask = CreateReductionQdsTask(JobDetail.Request.SelectionCriteria);

            // Run Qlikview publisher (QDS) task
            await RunQdsTask(ReductionTask);

            ReducedDocumentNode = await GetSourceDocumentNode(JobDetail.Request.RequestedOutputFileName, WorkingFolderRelative);

            if (ReducedDocumentNode == null)
            {
                JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.NoReducedFileCreated;
                string Msg = $"Failed to get DocumentNode for file {JobDetail.Request.RequestedOutputFileName} in folder {SourceDocFolder.General.Path}\\{WorkingFolderRelative}";
                throw new ApplicationException(Msg);
            }

            Log.Information($"Task {JobDetail.TaskId.ToString()} completed CreateReducedContent");
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

            File.Copy(ReducedFile, CopyDestinationPath, true);
            JobDetail.Result.ReducedContentFileChecksum = GlobalFunctions.GetFileChecksum(CopyDestinationPath);
            JobDetail.Result.ReducedContentFilePath = CopyDestinationPath;

            Log.Information($"Task {JobDetail.TaskId.ToString()} completed DistributeReducedContent");
        }

        /// <summary>
        /// Remove all temporary artifacts of the entire process
        /// </summary>
        private bool Cleanup()
        {
            string WorkingFolderAbsolute = Path.Combine(SourceDocFolder.General.Path, WorkingFolderRelative);
            if (!string.IsNullOrWhiteSpace(WorkingFolderRelative) && Directory.Exists(WorkingFolderAbsolute))
            {
                try
                {
                    FileSystemUtil.DeleteDirectoryWithRetry(WorkingFolderAbsolute);
                }
                catch (System.Exception e)  // Do not let this throw upward
                {
                    // It's an error, but the reduction task has completed by now so just log this and continue.
                    Log.Error(e, $"In QvReductionRunner.Cleanup(), failed to delete temporary reduction directory {WorkingFolderAbsolute}, continuing");
                }
            }

            Log.Information($"Task {JobDetail.TaskId.ToString()} completed Cleanup");

            return true;
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

            DocumentNode[] AllDocNodes = new DocumentNode[0];
            DateTime Start = DateTime.Now;
            while (DocNode == null && (DateTime.Now - Start) < new TimeSpan(0, 1, 10))  // QV server seems to poll for files every minute
            {
                Thread.Sleep(500);
                AllDocNodes = await _newQdsClient.GetSourceDocumentNodesAsync(QdsServiceInfo.ID, SourceDocFolder.ID, RequestedRelativeFolder);
                DocNode = AllDocNodes.SingleOrDefault(dn => dn.FolderID == SourceDocFolder.ID
                                                            && dn.Name == RequestedFileName
                                                            && dn.RelativePath == RequestedRelativeFolder);
            }

            if (DocNode == null)
            {
                // Don't throw here, caller can decide what to do
                Log.Error($"Did not find SourceDocument '{RequestedFileName}' in subfolder {RequestedRelativeFolder} of source documents folder {SourceDocFolder.General.Path}");
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
            DocumentTask NewDocumentTask = new DocumentTask();

            NewDocumentTask.ID = Guid.NewGuid();
            NewDocumentTask.QDSID = QdsServiceInfo.ID;
            NewDocumentTask.Document = DocNodeArg;

            #region General Tab
            NewDocumentTask.Scope |= DocumentTaskScope.General;
            NewDocumentTask.General = new DocumentTask.TaskGeneral();
            NewDocumentTask.General.Enabled = true;
            NewDocumentTask.General.TaskName = $"Hierarchy task {NewDocumentTask.ID} for job {JobDetail.TaskId.ToString("D")} at {TaskDateTimeStamp}";
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
        private DocumentTask CreateReductionQdsTask(IEnumerable<FieldValueSelection> Selections)
        {
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
            NewDocumentTask.General.TaskName = $"Reload and partial reduction task {NewDocumentTask.ID} for job {NewDocumentTask.ID.ToString("D")} at {TaskDateTimeStamp}";
            NewDocumentTask.General.TaskDescription = $"Automatically generated by ReductionService at {TaskDateTimeStamp}";
            #endregion

            #region reduce
            int NumSelectedValues = Selections.Count(v => v.Selected);

            NewDocumentTask.Scope |= DocumentTaskScope.Reduce;
            NewDocumentTask.Reduce = new DocumentTask.TaskReduce();
            NewDocumentTask.Reduce.DocumentNameTemplate = Path.GetFileNameWithoutExtension(JobDetail.Request.RequestedOutputFileName);
            NewDocumentTask.Reduce.Static = new DocumentTask.TaskReduce.TaskReduceStatic();
            NewDocumentTask.Reduce.Static.Reductions = new TaskReduction[NumSelectedValues];

            int Index = 0;
            foreach (FieldValueSelection FieldVal in Selections.Where(v => v.Selected))
            {
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
        private async Task RunQdsTask(DocumentTask DocTask, int? timeoutMinutes = null)
        {
            var defaultTimeout = int.Parse(Configuration.ApplicationConfiguration["DefaultQdsTaskTimeoutMinutes"]);
            TimeSpan MaxStartDelay = new TimeSpan(0, 0, 5, 0);
            TimeSpan MaxElapsedRun = new TimeSpan(0, 0, timeoutMinutes ?? defaultTimeout, 0);
            TimeSpan TaskStartPollingInterval = new TimeSpan(0, 0, 0, 10);
            TimeSpan PublisherPollingInterval = new TimeSpan(0, 0, 0, 1);

            QlikviewLib.Qms.TaskStatus Status = default;

            // Save the task to Qlikview server
            DateTime SaveStartTime = DateTime.Now;
            try
            {
                await _newQdsClient.SaveDocumentTaskAsync(DocTask);
            }
            catch (System.Exception ex)
            {
                throw new ApplicationException("QmsClient.SaveDocumentTaskAsync exception", ex);
            }

            TaskInfo TInfo = default;
            try
            {
                TInfo = await _newQdsClient.FindTaskAsync(QdsServiceInfo.ID, TaskType.DocumentTask, DocTask.General.TaskName);
            }
            catch (System.Exception ex)
            {
                throw new ApplicationException("After saving task, QmsClient.FindTaskAsync exception", ex);
            }
            Guid TaskIdGuid = TInfo.ID;
            Log.Information($"In QvReductionRunner.RunQdsTask() successfully saved task {TaskIdGuid.ToString("D")}, and retrieved task info, after {DateTime.Now - SaveStartTime}");

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
                        throw new System.Exception($"Qlikview publisher failed to start task {TaskIdGuid.ToString("D")} before timeout");
                    }

                    try
                    {
                        await _newQdsClient.RunTaskAsync(TaskIdGuid);
                    }
                    catch (System.Exception ex)
                    {
                        throw new ApplicationException("QmsClient.RunTaskAsync exception", ex);
                    }

                    Thread.Sleep(TaskStartPollingInterval);

                    try
                    {
                        Status = await _newQdsClient.GetTaskStatusAsync(TaskIdGuid, TaskStatusScope.All);
                    }
                    catch (System.Exception ex)
                    {
                        if (pollTaskStartRetryCount-- > 0)
                        {
                            Log.Information(ex, "Retrying after exception while polling for task status after RunTaskAsync");
                            continue;
                        }
                        throw new ApplicationException("Exceeded maximum retries for QmsClient.GetTaskStatusAsync while trying to start task", ex);
                    }
                } while (Status == null || Status.Extended == null || !(DateTime.TryParse(Status.Extended.StartTime, out _) || DateTime.TryParse(Status.Extended.FinishedTime, out _)));
                Log.Information($"In QvReductionRunner.RunQdsTask() task {TaskIdGuid.ToString("D")} started running after {DateTime.Now - RunStartTime}");

                // Wait for started task to finish
                DateTime RunningStartTime = DateTime.Now;
                int pollTaskFinishRetryCount = 3;
                do
                {
                    if (DateTime.Now - RunningStartTime > MaxElapsedRun)
                    {
                        JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.ReductionProcessingTimeout;
                        throw new System.Exception($"Qlikview publisher failed to finish task {TaskIdGuid.ToString("D")} before timeout");
                    }

                    Thread.Sleep(PublisherPollingInterval);

                    try
                    {
                        Status = await _newQdsClient.GetTaskStatusAsync(TaskIdGuid, TaskStatusScope.All);
                    }
                    catch (System.Exception ex)
                    {
                        if (pollTaskFinishRetryCount-- > 0)
                        {
                            Log.Information(ex, "Retrying after exception while polling for task status while task is running");
                            continue;
                        }
                        throw new ApplicationException("Exceeded maximum retries for QmsClient.GetTaskStatusAsync while waiting for task to finish", ex);
                    }
                } while (Status == null || Status.Extended == null || !DateTime.TryParse(Status.Extended.FinishedTime, out _));
                Log.Information($"In QvReductionRunner.RunQdsTask() task {TaskIdGuid.ToString("D")} finished running after {DateTime.Now - RunningStartTime}");

                switch (Status.General.Status)
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
                        throw new ApplicationException($"QDS status {Status.General.Status.ToString()} after task {TaskIdGuid.ToString("D")}:{Environment.NewLine}{Status.Extended.LastLogMessages}");

                    case TaskStatusValue.Failed:
                        JobDetail.Result.OutcomeReason = ReductionJobDetail.JobOutcomeReason.UnspecifiedError;
                        throw new ApplicationException($"QDS status {Status.General.Status.ToString()} after task {TaskIdGuid.ToString("D")}:{Environment.NewLine}{Status.Extended.LastLogMessages}");
                }
            }
            finally
            {
                // Clean up
                try
                {
                    Status = await _newQdsClient.GetTaskStatusAsync(TaskIdGuid, TaskStatusScope.All);
                }
                catch (System.Exception ex)
                {
                    throw new ApplicationException("QmsClient.GetTaskStatusAsync (in final cleanup) exception", ex);
                }

                // null would indicate that the task doesn't exist
                if (Status != null)
                {
                    try
                    {
                        await _newQdsClient.DeleteTaskAsync(TaskIdGuid, TInfo.Type);
                    }
                    catch (System.Exception ex)
                    {
                        throw new ApplicationException("QmsClient.DeleteTaskAsync exception", ex);
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

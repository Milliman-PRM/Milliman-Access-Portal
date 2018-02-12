
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Milliman.Reduction.ReductionEngine
{
    using Milliman.Common;
    using Milliman.Common.TreeUtilities;

    internal class ReductionRunnerException : Exception
    {
        internal ReductionRunnerException(string message) : base(message)
        { }
    }

    public class ReductionRunner
    {
        private static string C_LOG = "User Notification", C_CLEAN = "Clean Up", C_WRAP = "Wrap Up";
        private Guid QdsId;
        private Guid QvsId;
        private QMSConnection QmsCxn;
        private QMSAPI.DocumentFolder SourceDocumentFolder;
        //private QMSAPI.TaskInfo HierarchyTaskInfo;
        //StringBuilder _userMessage = new StringBuilder();

        private Dictionary<string, List<Action>> _late_execution = new Dictionary<string, List<Action>>();

        public Milliman.Common.ReduceConfig ConfigFileContent { get; set; }
        public string QVWOriginalFullFileName { get; set; }
        public QMSSettings QVConnectionSettings { get; set; }
        public List<string>[] MasterCandidateFiles { get; set; }

        public ReductionRunner()
        {
            this.QVConnectionSettings = new QMSSettings();
        }
        public ReductionRunner(QMSSettings settings) : this()
        {
            this.QVConnectionSettings = settings;

            // Get the source document folder on the QVS. This will be used in all our future iterations
            SourceDocumentFolder = GetSourceDocumentFolder();
        }

        /// <summary>
        /// Entry point to manage all the low level work associated with one Reduction Service config file.
        /// </summary>
        public void Run()
        {
            try
            {
                while (true)
                {
                    // Root hierarchy task. 
                    Guid RootHierarchyTaskId = Guid.NewGuid();

                    string RootWorkingPath = Path.Combine(SourceDocumentFolder.General.Path, RootHierarchyTaskId.ToString("N"));
                    Directory.CreateDirectory(RootWorkingPath);
                    //Trace.WriteLine(string.Format("Folder {0} created for root hierarchy task", ReductionWorkingPath));

                    string RootWorkingQvwFilePath = Path.Combine(RootWorkingPath, RootHierarchyTaskId.ToString("N") + ".qvw");
                    File.Copy(QVWOriginalFullFileName, RootWorkingQvwFilePath, true);
                    //Trace.WriteLine(string.Format("Copied original QVW File {0} to {1} for root hierarchy task", QVWOriginalFullFileName, RootWorkingQvwFilePath));

                    ExtractHierarchyFromExistingQvw(RootWorkingQvwFilePath, RootHierarchyTaskId);

                    foreach (ReductionSettings SelectionSet in this.ConfigFileContent.SelectionSets)
                    {
                        try
                        {
                            if (SelectionSet == null) continue;

                            this.GenerateTreeInfo(SelectionSet, RootWorkingPath);
                            if (string.IsNullOrEmpty(SelectionSet.ReducedQVWName) || SelectionSet.ReducedQVWName == "null") continue;

                            Guid ReductionTaskId = Guid.NewGuid();

                            string ReductionWorkingPath = Path.Combine(RootWorkingPath, ReductionTaskId.ToString("N"));
                            Directory.CreateDirectory(ReductionWorkingPath);
                            //Trace.WriteLine(string.Format("Folder {0} created for reduction task", ReductionWorkingPath));

                            string ReductionQvwFilePath = Path.Combine(ReductionWorkingPath, ReductionTaskId.ToString("N") + ".qvw");
                            File.Copy(QVWOriginalFullFileName, ReductionQvwFilePath, true);
                            //Trace.WriteLine(string.Format("Copied original QVW File {0} to {1} for reduction task", QVWOriginalFullFileName, ReductionQvwFilePath));

                            QMSAPI.DocumentTask reduction_document_task = this.CreateReductionTask(ReductionTaskId,
                                                                                                   ReductionQvwFilePath,
                                                                                                   SelectionSet.ReducedQVWName,
                                                                                                   SelectionSet.DropDisassociatedDataModelTables,
                                                                                                   SelectionSet.SelectionCriteria,
                                                                                                   SelectionSet.DropTables);
                            this.runReductionTask(reduction_document_task, true);

                            if ((SelectionSet.UniqueValuesFromMasterQVWColumns != null) && (SelectionSet.UniqueValuesFromMasterQVWColumns.Count > 0))
                                this.generateUniqueValuesForQVW(RootWorkingPath, SelectionSet.UniqueValuesFromMasterQVWColumns, SelectionSet, true);

                            if ((SelectionSet.UniqueValuesFromReducedQVWColumns != null) && (SelectionSet.UniqueValuesFromReducedQVWColumns.Count > 0))
                            {
                                Guid HierarchyTaskId = Guid.NewGuid();

                                string ReducedQvwWorkingPath = Path.Combine(RootWorkingPath, HierarchyTaskId.ToString("N"));
                                Directory.CreateDirectory(ReducedQvwWorkingPath);
                                //Trace.WriteLine(string.Format("Folder {0} created for reduced hierarchy task", ReductionWorkingPath));

                                string ReducedQvwFileName = Path.Combine(ReducedQvwWorkingPath, Path.GetFileName(SelectionSet.ReducedQVWName) + ".qvw");
                                File.Copy(RootWorkingQvwFilePath, ReducedQvwFileName, true);

                                ExtractHierarchyFromExistingQvw(ReducedQvwFileName, HierarchyTaskId);

                                this.generateUniqueValuesForQVW(ReducedQvwWorkingPath, SelectionSet.UniqueValuesFromReducedQVWColumns, SelectionSet);
                            }

                            flushLogMessages(SelectionSet, true, null);
                        }
                        catch (Exception ex)
                        {
                            flushLogMessages(SelectionSet, false, ex.Message);
                        }

                    }
                    break;
                }
            }
            catch (ReductionRunnerException ex)
            {
                Trace.WriteLine("ReductionRunnerException" + ex);  //catch and log the exception
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);  //catch and log the exception
            }

            this.ExecuteActions(C_WRAP);
            this.ExecuteActions(C_CLEAN);
            this.ExecuteActions(C_LOG);

            createCompleteFlags(this.ConfigFileContent.ConfigFileNameWithoutExtension);
        }

        private void ExtractHierarchyFromExistingQvw(string QvwFilePath, Guid HierarchyTaskId = new Guid(), string DestinationRelativeFolder = ".")
        {
            string WorkingFolder = Path.GetDirectoryName(QvwFilePath);

            string AncillaryScriptFilePath = Path.Combine(WorkingFolder, "ancillary_script.txt");
            Trace.WriteLine("Creating ancillary script " + AncillaryScriptFilePath);
            using (StreamWriter ancillary_writer = new StreamWriter(AncillaryScriptFilePath))
            {
                ancillary_writer.WriteLine("LET DataExtraction=true();");
            }
            AddCleanUpAction(AncillaryScriptFilePath);

            QMSAPI.TaskInfo HierarchyTaskInfo = CreateHierarchyTask(QvwFilePath, HierarchyTaskId);

            RunHierarchyTask(HierarchyTaskInfo);
        }

        /// <summary>
        /// Create a "GUID_completed.txt" file for each completed task
        /// Check to see if I am the last task in this group to execute, if so write out a "processing_complete.txt" file
        /// </summary>
        /// <param name="ConfigurationName"></param>
        private void createCompleteFlags(string ConfigurationName)
        {
            string complete_process_stream = Path.Combine(ConfigFileContent.ConfigFQFolderName, ConfigurationName + "_completed.txt");
            using (StreamWriter complete_process_writer = new StreamWriter(complete_process_stream))
            {
                complete_process_writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                complete_process_writer.Flush();
            }

            //Note: this code is experimental and does not always give the intended results.....
            //look to see if all the other tasks have completed, if so I am the last one, so write out the processing_complete.txt semaphore, all tasks are done
            string[] ConfigurationFiles = Directory.GetFiles(ConfigFileContent.ConfigFQFolderName, "*.config", SearchOption.TopDirectoryOnly);
            string[] CompletedFiles = Directory.GetFiles(ConfigFileContent.ConfigFQFolderName, "*_completed.txt", SearchOption.TopDirectoryOnly);
            if (ConfigurationFiles.Length == CompletedFiles.Length)
            {  //each config file has a completed file, thus we must be last task executing
                string all_done_stream = Path.Combine(ConfigFileContent.ConfigFQFolderName, "processing_complete.txt");
                using (StreamWriter all_done_writer = new StreamWriter(all_done_stream))
                {
                    all_done_writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    all_done_writer.Flush();
                }
            }
        }

        private void ExecuteActions(string category)
        {
            if (!_late_execution.ContainsKey(category)) return;
            foreach (var action in _late_execution[category])
            {
                try
                {
                    Trace.WriteLine(string.Format("Executing late action for '{0}':\r\n{1}", category, action.Method.ToString()));
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(string.Format("Fail to run late action '{0}':\r\nERROR: {1}", category, ex.Message));
                }
            }
            _late_execution[category].Clear();
        }

        private void flushLogMessages(ReductionSettings set, bool success, string errorMessage)
        {
            string master_log = Path.Combine(ConfigFileContent.ConfigFQFolderName, ConfigFileContent.MasterStatusLog);
            System.Diagnostics.Debug.WriteLine("master->" + ConfigFileContent.MasterStatusLog);
            System.Diagnostics.Debug.WriteLine("task->" + set.QVWStatusLog);
            if (!File.Exists(master_log))
            {
                using (File.Create(master_log)) { }
            }

            using (StreamWriter master_log_writer = File.AppendText(master_log))
            {
                master_log_writer.WriteLine(string.Format("{0}|{1}{2}",
                    ConfigFileContent.ConfigFileName,
                    success ? "SUCCESS" : "FAIL",
                    success ? "" : "|" + errorMessage));
                master_log_writer.Flush();
            }

            //VWN:check to see if there is a QVWStatusLog, when processing the master QVW to generate just hierarchies there may
            //not be a QVWStausLog - not an error condition
            if (!string.IsNullOrEmpty(set.QVWStatusLog))
            {
                string status_log = Path.Combine(ConfigFileContent.ConfigFQFolderName, set.QVWStatusLog);
                using (StreamWriter status_log_writer = File.AppendText(status_log))
                {
                    status_log_writer.WriteLine(string.Format("{0}", success ? "SUCCESS" : "FAIL"));
                    status_log_writer.Flush();
                }
            }
        }

        private QMSAPI.DocumentNode GetNode(string QvwName)
        {
            // Connect to Qlikview server
            QMSAPI.IQMS QmsClient = ConnectToQMS(this.QVConnectionSettings);

            QMSAPI.DocumentNode node = null;
            foreach (var doc_node in QmsClient.GetSourceDocuments(this.QdsId))
            {
                if (doc_node.Name == QvwName)
                {
                    node = doc_node;
                    break;
                }
            }
            if (node == null)
            {
                Trace.WriteLine(string.Format("Could not find node '{0}' on Publisher's source documents.", QvwName));
            }
            else
            {
                //Trace.WriteLine(string.Format("Successfully fetched SourceDocument '{0}' from Qlikview Publisher", qvwName));
            }
            return node;
        }

        /// <summary>
        /// 
        /// </summary>
        private void generateUniqueValuesForQVW(string sourceFolderNameGUID, List<NVPair> uniqueColumns, ReductionSettings set, bool isMaster = false)
        {
            TreeBuilderProcessor processor = new TreeBuilderProcessor();
            List<string> ColumnNames = new List<string>();
            foreach (NVPair Pair in uniqueColumns)
                ColumnNames.Add(Pair.FieldName);
            List<string> candidate_files = processor.FindCandidateFiles(sourceFolderNameGUID, ColumnNames);

            //VWN
            System.Diagnostics.Debug.WriteLine("Generating unique values - found " + candidate_files.Count.ToString() + " files to process");

            foreach (var candidate in candidate_files)
            {
                if (candidate_files == null) continue;
                // Figure out if the current file has any of the columns we want
                string[] columns = getColumnNamesFromFile(candidate);
                foreach (var pair in uniqueColumns)
                {
                    for (int i = 0; i < columns.Length; i++)
                        if (columns[i] == pair.FieldName)
                        {
                            generateUniqueValuesFile(candidate, i, Path.Combine(Path.GetDirectoryName(this.QVWOriginalFullFileName), Path.GetFileName(pair.Value)));
                            break;
                        }
                }
            }
        }

        private void generateUniqueValuesFile(string sourceFile, int index, string destinationFile)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            using (StreamReader reader = new StreamReader(sourceFile))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] columns = line.Split('|');
                    if (columns.Length <= index) continue;
                    values[columns[index].ToLower()] = columns[index];
                }
            }
            using (StreamWriter writer = new StreamWriter(destinationFile))
            {
                foreach (var key in values.Keys)
                    writer.WriteLine(values[key].Trim(new char[] { '"' }));
            }
        }

        private string[] getColumnNamesFromFile(string fileName)
        {
            using (StreamReader reader = new StreamReader(fileName))
            {
                return ("" + reader.ReadLine()).Split('|');
            }
        }

        /// <summary>
        /// Opens a connection to the Qlikview server
        /// </summary>
        /// <param name="QmsSettings"></param>
        private QMSAPI.IQMS ConnectToQMS(QMSSettings QmsSettings)
        {
            if (QmsSettings == null)
            {
                throw new Exception("Unable to establish connection to QMS: QMS Settings is null");
            }
            else if (string.IsNullOrEmpty((QmsSettings.QMSURL + "").Trim()))
            {
                throw new Exception("Unable to establish connection to QMS: the QMS address is empty");
            }
            else
            {
                Trace.WriteLine(string.Format("Connecting to QMS on '{0}'", QmsSettings.QMSURL));
            }

            try
            {
                QmsCxn = new QMSConnection();
                if (string.IsNullOrEmpty(QmsSettings.UserName))
                {
                    QmsCxn.Connect(QmsSettings.QMSURL);
                }
                else
                {
                    QmsCxn.Connect(QmsSettings.QMSURL, QmsSettings.UserName, QmsSettings.GetPassword());
                }

                return QmsCxn.QlikClient;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error attempting to connect to Qlikview Server: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Querys the Source Document Folder from the QlikView publisher service
        /// </summary>
        /// <returns></returns>
        internal QMSAPI.DocumentFolder GetSourceDocumentFolder()
        {
            // Connect to Qlikview server
            QMSAPI.IQMS QmsClient = ConnectToQMS(this.QVConnectionSettings);

            QMSAPI.DocumentFolder QmsFolder = null;
            try
            {
                var services = QmsClient.GetServices(QMSAPI.ServiceTypes.QlikViewDistributionService);
                if (services == null || services.Length == 0)
                {
                    throw new ReductionRunnerException("QMSAPI did not return any Publisher services.");
                }
                else
                {
                    QdsId = services[0].ID;
                    //Trace.WriteLine(string.Format("Found {0} QDS services. Working with publisher service '{0}', of id '{1}'", services.Length, services[0].Name, QdsId.ToString("N")));
                }

                services = QmsClient.GetServices(QMSAPI.ServiceTypes.QlikViewServer);
                if (services == null || services.Length == 0)
                {
                    throw new ReductionRunnerException("QMSAPI did not return any QVS services. Reduction can continue to run but no statistics will be available for the server or cluster");
                }
                else
                {
                    QvsId = services[0].ID;
                    //Trace.WriteLine(string.Format("Found {0} QVS services.  Working with service '{0}', of id '{1}'", services.Length, services[0].Name, QvsId.ToString("N")));
                }

                QMSAPI.DocumentFolder[] SourceFolders = QmsClient.GetSourceDocumentFolders(QdsId, QMSAPI.DocumentFolderScope.All);
                if (SourceFolders == null || SourceFolders.Length == 0)
                {
                    throw new ReductionRunnerException("No Source Document Folder reported by the QDS service. ");
                }

                if (SourceFolders.Length > 1)
                {
                    Trace.WriteLine(SourceFolders.Length + " Document Folders were found on the QDS. Searching for a Milliman path");
                    foreach (var p in SourceFolders)
                    {
                        Trace.WriteLine(string.Format("Searching for 'Milliman' string in '{0}' path...", p.General.Path));
                        if (p.General.Path.ToLower().IndexOf("milliman") > 0)
                        {
                            QmsFolder = p;
                            break;
                        }
                    }
                    if (QmsFolder == null)
                    {
                        QmsFolder = SourceFolders[0];
                    }
                }
                else if (SourceFolders.Length == 1)
                {
                    // The normal path
                    QmsFolder = SourceFolders[0];
                }

                Trace.WriteLine(string.Format("Working path set to '{0}'", QmsFolder.General.Path));
                return QmsFolder;
            }
            catch (Exception ex)
            {
                throw new ReductionRunnerException("Error attempting to get Source Documents Folder from QDS: " + ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qvwDocumentPath">The full path of the reduced qvw that this task is to create</param>
        /// <param name="HierarchyTaskId"></param>
        /// <returns></returns>
        private QMSAPI.TaskInfo CreateHierarchyTask(string qvwDocumentPath, Guid HierarchyTaskId = new Guid())
        {
            // Connect to Qlikview server
            QMSAPI.IQMS QmsClient = ConnectToQMS(this.QVConnectionSettings);

            QMSAPI.TaskInfo TaskInfo = null;
            try
            {
                string qvw_file_name = Path.GetFileName(qvwDocumentPath);
                Trace.WriteLine(string.Format("Creating task for Qlikview Document '{0}'", qvwDocumentPath));
                QmsClient.ClearQVSCache(QMSAPI.QVSCacheObjects.All);
                Trace.WriteLine("QVSCache successfully cleared");

                Trace.WriteLine("Searching for correct document node on Publisher's Source Documents");
                QMSAPI.DocumentNode Node = GetNode(qvw_file_name);
                if (Node == null)
                {
                    Trace.WriteLine(string.Format("Could not find node '{0}' on Publisher's source documents. The process will be terminated", qvw_file_name));
                    throw new ReductionRunnerException(string.Format("Could not find node '{0}' on Publisher's source documents. The process will be terminated", qvw_file_name));
                }

                QMSAPI.DocumentTask temp_task = QMSWrapper.GetTask();

                Trace.WriteLine("Setting up hierarchy task properties");
                temp_task.ID = HierarchyTaskId;
                temp_task.QDSID = this.QdsId;
                temp_task.Document = Node;

                #region General Tab
                Trace.WriteLine("Configuring General Tab");
                temp_task.General = new QMSAPI.DocumentTask.TaskGeneral();
                temp_task.General.Enabled = true;
                temp_task.General.TaskName = "Reduction | Partial Reload | " + qvw_file_name.ToUpper();
                temp_task.General.TaskDescription = string.Format("Automatically generated by ReductionService on {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                #endregion

                #region Reload Tab
                Trace.WriteLine("Configuring Reload Tab");
                temp_task.Scope |= QMSAPI.DocumentTaskScope.Reload;
                temp_task.Reload = new QMSAPI.DocumentTask.TaskReload();
                temp_task.Reload.Mode = QMSAPI.TaskReloadMode.Partial;
                #endregion

                #region Reduce Tab
                Trace.WriteLine("Configuring Reduce Tab");
                temp_task.Scope |= QMSAPI.DocumentTaskScope.Reduce;
                temp_task.Reduce = new QMSAPI.DocumentTask.TaskReduce();
                temp_task.Reduce.DocumentNameTemplate = string.Empty;
                temp_task.Reduce.Static = new QMSAPI.DocumentTask.TaskReduce.TaskReduceStatic();
                temp_task.Reduce.Static.Reductions = new QMSAPI.TaskReduction[] { new QMSAPI.TaskReduction() };
                temp_task.Reduce.Static.Reductions[0].Type = new QMSAPI.TaskReductionType();
                temp_task.Reduce.Dynamic = new QMSAPI.DocumentTask.TaskReduce.TaskReduceDynamic();
                temp_task.Reduce.Dynamic.Type = new QMSAPI.TaskReductionType();
                #endregion

                #region Distribution Tab // not used for hierarchy task
                //Trace.WriteLine("Configuring Distribution Tab...");
                //temp_task.Scope |= QMSAPI.DocumentTaskScope.Distribute;
                //temp_task.Distribute = new QMSAPI.DocumentTask.TaskDistribute();
                //temp_task.Distribute.Output = new QMSAPI.DocumentTask.TaskDistribute.TaskDistributeOutput();
                //temp_task.Distribute.Output.Type = QMSAPI.TaskDistributionOutputType.QlikViewDocument;

                //temp_task.Distribute.Static = new QMSAPI.DocumentTask.TaskDistribute.TaskDistributeStatic();
                //temp_task.Distribute.Static.DistributionEntries = new QMSAPI.TaskDistributionEntry[] { new QMSAPI.TaskDistributionEntry() };
                //temp_task.Distribute.Static.DistributionEntries[0].Recipients = new QMSAPI.DirectoryServiceObject[] { new QMSAPI.DirectoryServiceObject() };
                //temp_task.Distribute.Static.DistributionEntries[0].Recipients[0].Type = QMSAPI.DirectoryServiceObjectType.Authenticated;
                //temp_task.Distribute.Static.DistributionEntries[0].Destination = new QMSAPI.TaskDistributionDestination();
                //temp_task.Distribute.Static.DistributionEntries[0].Destination.Folder = new QMSAPI.TaskDistributionDestination.TaskDistributionDestinationFolder();
                //temp_task.Distribute.Static.DistributionEntries[0].Destination.Type = QMSAPI.TaskDistributionDestinationType.None;
                //temp_task.Distribute.Static.DistributionEntries[0].Destination.Folder.Name = "";

                //temp_task.Distribute.Notification = new QMSAPI.DocumentTask.TaskDistribute.TaskDistributeNotification();
                //temp_task.Distribute.Notification.SendNotificationEmail = false;

                //temp_task.Distribute.Dynamic = new QMSAPI.DocumentTask.TaskDistribute.TaskDistributeDynamic();
                //temp_task.Distribute.Dynamic.IdentityType = QMSAPI.UserIdentityValueType.DisplayName;
                //temp_task.Distribute.Dynamic.Destinations = new QMSAPI.TaskDistributionDestination[] { new QMSAPI.TaskDistributionDestination() };
                #endregion

                #region Trigger Tab
                Trace.WriteLine("Configuring task trigger ...");
                temp_task.Scope |= QMSAPI.DocumentTaskScope.Triggering;
                temp_task.Triggering = new QMSAPI.DocumentTask.TaskTriggering();
                temp_task.Triggering.ExecutionAttempts = 1;
                temp_task.Triggering.ExecutionTimeout = 1440;
                temp_task.Triggering.TaskDependencies = new QMSAPI.TaskInfo[] { };
                temp_task.Triggering.Triggers = new QMSAPI.ScheduleTrigger[] {
                    new QMSAPI.ScheduleTrigger()
                };
                temp_task.Triggering.Triggers[0].Enabled = true;
                temp_task.Triggering.Triggers[0].Type = QMSAPI.TaskTriggerType.OnceTrigger;
                ((QMSAPI.ScheduleTrigger)temp_task.Triggering.Triggers[0]).StartAt = DateTime.Now;
                #endregion

                Trace.WriteLine("Saving hierarchy task");
                QmsClient.SaveDocumentTask(temp_task);
                while ((TaskInfo = QmsClient.FindTask(QdsId, QMSAPI.TaskType.DocumentTask, temp_task.General.TaskName)) == null)
                {
                    System.Threading.Thread.Sleep(500);
                }

                Trace.WriteLine(string.Format("Task with ID '{0}' successfully saved", temp_task.ID.ToString("N")));
            }
            catch (Exception ex)
            {
                //LogToUser("Error when trying to setup task on Qlikview Server: {0}", ex.Message);
                Trace.WriteLine(string.Format("An error occurred while setting up the hierarchy task on the Publisher server. The process will be terminated... {0}", ex));
                throw new ReductionRunnerException(string.Format("An error occurred while setting up the hierarchy task on the Publisher server. The process will be terminated... {0}", ex));
            }

            return TaskInfo;
        }

        private void RunHierarchyTask(QMSAPI.TaskInfo task)
        {
            try
            {
                DateTime StartTime = DateTime.Now;

                this.RunTaskSynchronously(task.ID);

                this.AddCleanUpAction(new Action(() => {
                    QMSAPI.IQMS QmsClient = ConnectToQMS(this.QVConnectionSettings);

                    Trace.WriteLine(string.Format("Deleting task '{0}'", task.ID.ToString("N")));
                    QmsClient.DeleteTask(task.ID, QMSAPI.TaskType.DocumentTask);
                }));

                DateTime EndTime = DateTime.Now;

                //LogToUser("Hierarchy Task ran on Qlikview Server, duration " + (EndTime-StartTime).ToString("g"));
                Trace.WriteLine("Hierarchy Task ran on Qlikview Server, duration " + (EndTime - StartTime).ToString("g"));
            }
            catch (Exception ex)
            {
                //LogToUser("Error when running the Hierarchy task on Qlikview Server: {0}", ex.Message);
                Trace.WriteLine(string.Format("An error occurred running the Hierarchy task... {0}", ex));
                throw new ReductionRunnerException(string.Format("An error occurred running the Hierarchy task... {0}", ex));
            }
        }

        private void GenerateTreeInfo(ReductionSettings SelectionSet, string workingFolder)
        {
            try
            {
                List<string> list_hierarchy, list_concept, list_drop, list_model;
                MasterCandidateFiles = new List<string>[4];
                TreeBuilderProcessor tree_processor = new TreeBuilderProcessor();
                Trace.WriteLine("Beginning TreeBuild processing...");
                if (!tree_processor.BuildTree(workingFolder, out list_hierarchy, out list_concept, out list_drop, out list_model))
                {
                    Trace.WriteLine("Unable to process ");
                    throw new ReductionRunnerException("Error executing BuildTree");
                }
                MasterCandidateFiles[0] = list_hierarchy;
                MasterCandidateFiles[1] = list_concept;
                MasterCandidateFiles[2] = list_drop;
                MasterCandidateFiles[3] = list_model;

                string destination_folder = Path.GetDirectoryName(this.QVWOriginalFullFileName);
                //string source_folder = this.QVWHierarchyFQFolderNameGUID, file_name = string.Empty;
                string tree_file_folder = Path.GetTempPath();

                if (!string.IsNullOrEmpty(SelectionSet.HierarchyFile))
                {
                    Trace.WriteLine("Adding backlog file move for HierarchyFile");
                    this.AddWrapUpAction(Path.Combine(tree_file_folder, list_hierarchy[0]), Path.Combine(destination_folder, Path.GetFileName(SelectionSet.HierarchyFile)), false);
                    this.AddCleanUpAction(Path.Combine(tree_file_folder, list_hierarchy[0]), "Hierarchy file for " + Path.GetDirectoryName(workingFolder));
                }
                if (!string.IsNullOrEmpty(SelectionSet.ConceptFile))
                {
                    Trace.WriteLine("Adding backlog file move for ConceptFile");
                    this.AddWrapUpAction(Path.Combine(tree_file_folder, list_concept[0]), Path.Combine(destination_folder, Path.GetFileName(SelectionSet.ConceptFile)), false);
                    this.AddCleanUpAction(Path.Combine(tree_file_folder, list_concept[0]), "Concept file for " + Path.GetDirectoryName(workingFolder));
                }

                if (!string.IsNullOrEmpty(SelectionSet.MapFile))
                {
                    Trace.WriteLine("Adding backlog file move for MapFile");
                    this.AddWrapUpAction(Path.Combine(tree_file_folder, list_drop[0]), Path.Combine(destination_folder, Path.GetFileName(SelectionSet.MapFile)), false);
                    this.AddCleanUpAction(Path.Combine(tree_file_folder, list_drop[0]), "Drop file for " + Path.GetDirectoryName(workingFolder));
                }

                if (!string.IsNullOrEmpty(SelectionSet.DataModelFile))
                {
                    Trace.WriteLine("Adding backlog file move for DataModelFile");
                    this.AddWrapUpAction(Path.Combine(tree_file_folder, list_model[0]), Path.Combine(destination_folder, Path.GetFileName(SelectionSet.DataModelFile)), false);
                    this.AddCleanUpAction(Path.Combine(tree_file_folder, list_model[0]), "Model file for " + Path.GetDirectoryName(workingFolder));
                }

                //LogToUser("Generate TreeInfo successfully generated intermediate data files");
            }
            catch (Exception ex)
            {
                //LogToUser("Unable to Generate Tree Info: {0}", ex.Message);
                Trace.WriteLine(string.Format("Unable to generate tree files {0}", ex));
                throw new ReductionRunnerException(string.Format("Unable to generate tree files {0}", ex));
            }
        }

        private QMSAPI.DocumentTask CreateReductionTask(Guid ReductionTaskId, string QvwFileToReducePath, string ReducedQvwFileName, bool shouldDropDisassociatedTables, List<NVPair> selectionCriteria, List<string> tablesToDrop)
        {
            try
            {
                string TaskFolder = Path.GetDirectoryName(QvwFileToReducePath);
                string FileToReducePath = Path.Combine(TaskFolder, QvwFileToReducePath);

                Trace.WriteLine("Processing Drop Tables...");
                DateTime start = DateTime.Now;
                bool drop_required = ReductionHelper.dropTableProcessor(TaskFolder,
                    shouldDropDisassociatedTables,
                    this.ConfigFileContent.MasterQVW,
                    selectionCriteria,
                    tablesToDrop);
                System.Threading.Thread.Sleep(2000);

                //delete any previous reduced files
                //System.IO.File.Delete(ReducedQVWName);

                Trace.WriteLine("Searching for correct document node on Publisher's Source Documents");
                QMSAPI.DocumentNode Node = GetNode(Path.GetFileName(QvwFileToReducePath));
                if (Node == null)
                {
                    Trace.WriteLine(string.Format("Could not find node '{0}' on Publisher's source documents. The process will be terminated ...", QvwFileToReducePath));
                    return null;
                }

                Trace.WriteLine("Setting up reduction task properties");
                QMSAPI.DocumentTask temp_task = QMSWrapper.GetTask();

                //Creates the Reduction Task
                QMSAPI.DocumentTask task_reduction = new QMSAPI.DocumentTask();

                task_reduction.ID = ReductionTaskId;
                task_reduction.QDSID = QdsId;
                task_reduction.Document = Node;
                task_reduction.Scope = QMSAPI.DocumentTaskScope.General;
                task_reduction.Scope |= QMSAPI.DocumentTaskScope.Reduce;
                task_reduction.Scope |= QMSAPI.DocumentTaskScope.Distribute;
                task_reduction.Scope |= QMSAPI.DocumentTaskScope.Reload;

                #region general
                task_reduction.General = new QMSAPI.DocumentTask.TaskGeneral();
                task_reduction.General.Enabled = true;
                task_reduction.General.TaskName = "Reload & Reduce " + QvwFileToReducePath;
                task_reduction.General.TaskDescription = "Performs reduction on " + QvwFileToReducePath;
                #endregion

                #region reduce
                task_reduction.Reduce = new QMSAPI.DocumentTask.TaskReduce();
                task_reduction.Reduce.DocumentNameTemplate = Path.GetFileNameWithoutExtension(ReducedQvwFileName);
                task_reduction.Reduce.Static = new QMSAPI.DocumentTask.TaskReduce.TaskReduceStatic();
                task_reduction.Reduce.Static.Reductions = new QMSAPI.TaskReduction[selectionCriteria.Count];
                int Index = 0;
                foreach (var selection in selectionCriteria)
                {
                    Trace.WriteLine(string.Format("For task {0}, selection key: {1} with value {2}", ReductionTaskId.ToString("N"), selection.FieldName, selection.Value)); // TODO delete this
                    task_reduction.Reduce.Static.Reductions[Index] = new QMSAPI.TaskReduction();
                    task_reduction.Reduce.Static.Reductions[Index].Type = QMSAPI.TaskReductionType.ByField;
                    task_reduction.Reduce.Static.Reductions[Index].Field = new QMSAPI.TaskReduction.TaskReductionField();
                    task_reduction.Reduce.Static.Reductions[Index].Field.Name = selection.FieldName;
                    task_reduction.Reduce.Static.Reductions[Index].Field.Value = selection.Value;
                    task_reduction.Reduce.Static.Reductions[Index].Field.IsNumeric = selection.IsNumeric;
                    Index++;
                }

                task_reduction.Reduce.Dynamic = new QMSAPI.DocumentTask.TaskReduce.TaskReduceDynamic();
                #endregion

                #region distribute
                task_reduction.Distribute = new QMSAPI.DocumentTask.TaskDistribute();
                task_reduction.Distribute.Output = new QMSAPI.DocumentTask.TaskDistribute.TaskDistributeOutput();
                task_reduction.Distribute.Output.Type = QMSAPI.TaskDistributionOutputType.QlikViewDocument;

                task_reduction.Distribute.Static = new QMSAPI.DocumentTask.TaskDistribute.TaskDistributeStatic();
                task_reduction.Distribute.Static.DistributionEntries = new QMSAPI.TaskDistributionEntry[1];
                task_reduction.Distribute.Static.DistributionEntries[0] = new QMSAPI.TaskDistributionEntry();
                task_reduction.Distribute.Static.DistributionEntries[0].Recipients = new QMSAPI.DirectoryServiceObject[1];
                task_reduction.Distribute.Static.DistributionEntries[0].Recipients[0] = new QMSAPI.DirectoryServiceObject();
                task_reduction.Distribute.Static.DistributionEntries[0].Recipients[0].Type = QMSAPI.DirectoryServiceObjectType.Authenticated;
                task_reduction.Distribute.Static.DistributionEntries[0].Destination = new QMSAPI.TaskDistributionDestination();
                task_reduction.Distribute.Static.DistributionEntries[0].Destination.Folder = new QMSAPI.TaskDistributionDestination.TaskDistributionDestinationFolder();
                task_reduction.Distribute.Static.DistributionEntries[0].Destination.Type = QMSAPI.TaskDistributionDestinationType.Folder;
                task_reduction.Distribute.Static.DistributionEntries[0].Destination.Folder.Name = System.IO.Path.GetDirectoryName(QvwFileToReducePath);

                task_reduction.Distribute.Notification = new QMSAPI.DocumentTask.TaskDistribute.TaskDistributeNotification();
                task_reduction.Distribute.Notification.SendNotificationEmail = false;

                task_reduction.Distribute.Dynamic = new QMSAPI.DocumentTask.TaskDistribute.TaskDistributeDynamic();
                task_reduction.Distribute.Dynamic.IdentityType = QMSAPI.UserIdentityValueType.DisplayName;
                task_reduction.Distribute.Dynamic.Destinations = new QMSAPI.TaskDistributionDestination[0];
                #endregion

                #region reload
                task_reduction.Reload = new QMSAPI.DocumentTask.TaskReload();
                if (drop_required)
                    task_reduction.Reload.Mode = QMSAPI.TaskReloadMode.Partial; //this will result in tables being dropped as defined by the drop table processor
                else
                    task_reduction.Reload.Mode = QMSAPI.TaskReloadMode.None;
                #endregion

                #region trigger
                //documentTask.DocumentInfo = new DocumentTask.TaskDocumentInfo();
                task_reduction.Triggering = new QMSAPI.DocumentTask.TaskTriggering();
                task_reduction.Triggering.ExecutionAttempts = 1;
                task_reduction.Triggering.ExecutionTimeout = 1440;
                task_reduction.Triggering.TaskDependencies = new QMSAPI.TaskInfo[] { };

                task_reduction.Triggering.Triggers = new QMSAPI.ScheduleTrigger[1];
                var trigger_schedule = new QMSAPI.ScheduleTrigger();
                trigger_schedule.Enabled = true;
                trigger_schedule.Type = QMSAPI.TaskTriggerType.OnceTrigger;
                trigger_schedule.StartAt = DateTime.Now.AddSeconds(5.0); //don't schedule for now, it ignores if in past
                task_reduction.Triggering.Triggers[0] = trigger_schedule;
                #endregion

                QMSAPI.IQMS QmsClient = ConnectToQMS(this.QVConnectionSettings);
                QmsClient.SaveDocumentTask(task_reduction);

                System.Threading.Thread.Sleep(4000);//give it a second

                return task_reduction;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("An error occurred when trying to create or save the Reduction task... {0}", ex));
                return null;
            }
        }

        private void runReductionTask(QMSAPI.DocumentTask task, bool cleanUpWhenDone)
        {
            try
            {
                Trace.WriteLine(string.Format("Attempting to run newly created reduction task '{0}' ...", task.ID.ToString("N")));
                DateTime start_time = DateTime.Now;
                /*******************************************************************************************/
                this.RunTaskSynchronously(task.ID);
                /*******************************************************************************************/
                DateTime end_time = DateTime.Now;

                string remote_reduced_qvw_fq_file_name = Path.Combine(task.Distribute.Static.DistributionEntries[0].Destination.Folder.Name, task.Reduce.DocumentNameTemplate + ".qvw");
                string local_reduced_qvw_fq_file_name = Path.Combine(Path.GetDirectoryName(this.QVWOriginalFullFileName), Path.GetFileName(remote_reduced_qvw_fq_file_name));

                if (File.Exists(local_reduced_qvw_fq_file_name))
                {
                    Trace.WriteLine(string.Format("File '{0}' already exists so it'll be deleted before a copy is attempted...", local_reduced_qvw_fq_file_name));
                    File.Delete(local_reduced_qvw_fq_file_name);
                }
                Trace.WriteLine(string.Format("Copying reduced qvw from '{0}' back into the source folder '{1}'", remote_reduced_qvw_fq_file_name, local_reduced_qvw_fq_file_name));
                this.AddWrapUpAction(remote_reduced_qvw_fq_file_name, local_reduced_qvw_fq_file_name, false);

                this.AddCleanUpAction(new Action(() =>
                {
                    QMSAPI.IQMS QmsClient = ConnectToQMS(this.QVConnectionSettings);

                    Trace.WriteLine(string.Format("Deleting task '{0}'", task.ID.ToString("N")));
                    QmsClient.DeleteTask(task.ID, QMSAPI.TaskType.DocumentTask);
                    Trace.WriteLine(string.Format("Deleting original file '{0}'", task.General));
                    Directory.Delete(Path.GetDirectoryName(remote_reduced_qvw_fq_file_name), true);
                }));

                //LogToUser("Successfully ran reduction task [id: {0}; name: \"{1}\"] for current selection set.", task.ID, task.General.TaskName);
            }
            catch (Exception ex)
            {
                //LogToUser("Unable to run Reduction Task [id: {0}; name: {1}]: {2}", task.ID, task.General.TaskName, ex.Message);
                Trace.WriteLine(string.Format("An error occurred when trying to run the Reduction task... {0}", ex));
                throw new ReductionRunnerException(string.Format("An error occurred when trying to run the Reduction task... {0}", ex));
            }
        }

        /// <summary>
        /// Initiates and monitors the running task, and returns when it's over
        /// </summary>
        /// <param name="taskId"></param>
        private void RunTaskSynchronously(Guid taskId)
        {
            QMSAPI.TaskStatus status = null;
            int SecondsCounter = 0;
            string TaskIdStr = taskId.ToString("N");
            const int RunTaskIntervalSeconds = 5;
            const int CheckTaskIntervalSeconds = 2;
            DateTime ParsedTime;

            // Get the task running.  It generally takes more than 1 attempt.  
            while (status == null || status.Extended == null || !DateTime.TryParse(status.Extended.StartTime, out ParsedTime))
            {
                // Connect to Qlikview server (and refresh service key)
                QMSAPI.IQMS QmsClient = ConnectToQMS(this.QVConnectionSettings);

                QmsClient.RunTask(taskId);

                System.Threading.Thread.Sleep(RunTaskIntervalSeconds * 1000);
                SecondsCounter += RunTaskIntervalSeconds;

                status = QmsClient.GetTaskStatus(taskId, QMSAPI.TaskStatusScope.All);
                Trace.WriteLine(string.Format("Task {0} has status {1} after _qms_client.RunTask(), for {2} seconds", TaskIdStr, status.General.Status.ToString(), SecondsCounter));
            }

            SecondsCounter = 0;

            // loop until task is completed.  Most reliable signal of completion is FinishedTime
            while (string.IsNullOrEmpty(status.Extended.FinishedTime) || !DateTime.TryParse(status.Extended.FinishedTime, out ParsedTime))
            {
                // Connect to Qlikview server (and refresh service key)
                QMSAPI.IQMS QmsClient = ConnectToQMS(this.QVConnectionSettings);

                Trace.WriteLine(string.Format("Task {0} running {1} seconds, status {2}, FinishedTime {3}", TaskIdStr, SecondsCounter, status.General.Status.ToString(), ParsedTime == DateTime.MinValue ? "<none>" : ParsedTime.ToString()));

                System.Threading.Thread.Sleep(CheckTaskIntervalSeconds * 1000);
                SecondsCounter += CheckTaskIntervalSeconds;

                status = QmsClient.GetTaskStatus(taskId, QMSAPI.TaskStatusScope.All);
            }

            string Msg = string.IsNullOrEmpty(status.Extended.LastLogMessages) ? "" : status.Extended.LastLogMessages;
            Trace.WriteLine(string.Format("Task {0} finished at server time {1}. Current status is '{2}' with exit message '{3}'", TaskIdStr, status.Extended.FinishedTime, status.General.Status.ToString(), Msg));
        }

        #region Late Execution methods

        private void backLog(string category, Action action)
        {
            if (!_late_execution.ContainsKey(category))
                _late_execution.Add(category, new List<Action>());
            _late_execution[category].Add(action);
        }

        private void AddWrapUpAction(string source, string destination, bool deleteSource)
        {
            // VWN:when wrapped as an action the code never seems to execute
            // DPJ: Yeap, it's being called now, with the Wrap Up and User Log one
            this.AddWrapUpAction(() => {
                try
                {
                    File.Copy(source, destination, true);
                    Trace.WriteLine(string.Format("File '{0}' successfully copied", destination));
                    if (deleteSource)
                    {
                        Trace.WriteLine(string.Format("Deleting file '{0}'", source));
                        File.Delete(source);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(string.Format("Failed to copy file '{0}',\r\n{1}", destination, ex.Message));
                }
            });
        }

        private void AddCleanUpAction(string file, string description = "")
        {
            this.AddCleanUpAction(() => {
                try
                {
                    Trace.WriteLine(string.Format("Removing file '{0}'", file));
                    if (File.Exists(file))
                        File.Delete(file);
                    else
                        Trace.WriteLine("File does not exist...");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(string.Format("Unable to clean up file '{0}' - {1}\r\n{2}", file, description, ex.Message));
                }
            });
        }

        private void AddCleanUpAction(Action action)
        {
            this.backLog(C_CLEAN, action);
        }

        private void AddWrapUpAction(Action action)
        {
            this.backLog(C_WRAP, action);
        }

        //private void LogToUser(string message, params object[] args)
        //{
        //    LogToUser(string.Format(message, args));
        //}
        //private void LogToUser(string message)
        //{
        //    _userMessage.Append(string.Format("{0} [USER] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message));
        //}
        #endregion

        public Dictionary<string, int> GetQVSDocumentsPerUser()
        {
            // Connect to Qlikview server
            QMSAPI.IQMS QmsClient = ConnectToQMS(this.QVConnectionSettings);

            if (QvsId != Guid.Empty)
            {
                return QmsClient.GetQVSDocumentsPerUser(QvsId, QMSAPI.QueryTarget.Resource);
            }
            else
            {
                throw new InvalidDataException("Could not get QVS data because, during service initialization, we could not fetch a QVS GUID to work with. Please contact the administrator...");
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceModel;
using System.IO;
using Milliman.Common;

namespace Milliman.Reduction.ReductionEngine {

    /// <summary>
    /// Reduction Service class. Will spawn and manage threads for folders sent via Enqueue method
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ReductionService: IReductionService {
        private static Queue<KeyValuePair<string, ReduceConfig>> _queue = new Queue<KeyValuePair<string, ReduceConfig>>();
        private static AutoResetEvent _eventHasJobs = new AutoResetEvent(false);
        private static AutoResetEvent _eventJobFinished = new AutoResetEvent(true);
        
        private static Dictionary<string, Thread> _configWorkerPool = new Dictionary<string, Thread>();

        // Thread that manages the jobs in the background
        private static bool _stopFlag = false;
        private static Thread _JobProcessor;
        private static readonly object _LockQueue = new object();

        public ReductionService() {
            // Creates thread that will process jobs and requests
            this.Init();
        }
        public void Init() {
            this.InitThread();
        }
        private void InitThread() {
            if(_JobProcessor == null ) {
                Trace.WriteLine("Initializing job processor thread ...");
                _JobProcessor = new Thread(MonitorReductionJobs);
                _JobProcessor.IsBackground = true;
                _JobProcessor.Name = "QueueProcessorThread";
                _JobProcessor.Start();
                Trace.WriteLine("JobProcessor thread successfully intiated...");
            }
        }

        private void RestoreState() {
            // Picks up jobs where they left when the service stopped
        }

        /// <summary>
        /// Queue up a folder path to be assynchronously processed by the Service
        /// </summary>
        /// <param name="folderPath">The UNC path or Local path where the Config files and QVW meant to be processed resides</param>
        public void EnqueueReductionFolder(string folderPath) {
            Trace.WriteLine("Entering MM.ReductSvc::EnqueueReductionFolder() method");
            Trace.WriteLine(string.Format("ReductionService::EnqueueReductionFolder() received 'folderPath' as '{0}'", folderPath));

            string workingPath;
            if(string.IsNullOrEmpty(workingPath = GetWorkingPath(folderPath)) ) return;
            if( !IsSemaphoresOk(Path.Combine( workingPath,"request_complete.txt" ))) return;
            if( !IsConfigFileOk(workingPath)) return;

            Trace.WriteLine("Finished MM.ReductSvc::EnqueueReductionFolder() method");
        }

        /// <summary>
        /// Method that reads the queue and creates jobs for the files found there.
        /// If no file's found, flag the Service to wait until something arrives instead of polling the queue.
        /// </summary>
        private static void MonitorReductionJobs() {
            while( !_stopFlag ) {
                int threadCount = GetThreadThreshold(); // TODO: Get the ThreadCount from some settings file or call
                // This needs to be re-defined on every loop because we'll send this pointer to the newly created thread.
                KeyValuePair<string, ReduceConfig> _current = default(KeyValuePair<string, ReduceConfig>);

                if( _queue.Count == 0 ) {
                    Trace.WriteLine("No files found in the queue. Entering sleep mode...");
                    _eventHasJobs.WaitOne();
                    Trace.WriteLine("Somebody woke me up. Let me see what we have in the queue."); 
                    continue;
                } else if( _configWorkerPool.Count >= threadCount ) {
                    Trace.WriteLine("Amount of accumulated jobs is bigger than what I can handle. Will return once some work's finished.");
                    _eventJobFinished.WaitOne();
                    Trace.WriteLine("Finished some jobs. Checking if there are more waiting for me..");
                    continue;
                }

                // Checks the Queue... if we have something in there, add it up to the JobRunner threadpool
                lock ( _queue )
                   if( _queue.Count > 0 ) {
                        _current = _queue.Dequeue();
                        if( string.IsNullOrEmpty(_current.Key) ) continue;
                        Trace.WriteLine(string.Format("Found the '{0}' config file in the queue.", Path.GetFileName( _current.Key)));
                    }

                // Shoots up a thread for the current config file, and let it do the work for us...
                if( _configWorkerPool.Keys.Contains(_current.Key) ) {
                    Trace.WriteLine(string.Format("A duplicate request was received for file '{0}'. It will be ignored as a process is already handling it...", _current.Key));
                    continue;
                }

                Trace.WriteLine(string.Format("Creating worker thread for file '{0}'", Path.GetFileName(_current.Key)));
                Thread workerThread = new Thread(() => {
                    InitiateReduction(_current.Key, _current.Value);
                    FinishProcess(_current.Key);
                    _configWorkerPool.Remove(_current.Key);
                    Trace.WriteLine(string.Format("Just removed thread for '{0}'. The dictionary now contains {1} threads running.", _current.Key, _configWorkerPool.Count));
                    _eventJobFinished.Set();
                });
                workerThread.Name = Path.GetFileNameWithoutExtension(_current.Key);
                workerThread.IsBackground = true;
                _configWorkerPool.Add(_current.Key, workerThread);
                Trace.WriteLine("New thread succefully added to the pool...");
                // We only start after adding to our pool... 
                _configWorkerPool[_current.Key].Start();
                Trace.WriteLine(String.Format("Thread for config file '{0}' succefully started", Path.GetFileName(_current.Key)));
                Thread.Sleep(500);
            }
            // Serializes working-set so we can pick it back up when service restarts;
        }

        private static int GetThreadThreshold() {
            return 10;
        }

        /// <summary>
        /// Performs initial validation of all conditions necessary to run a reduction.
        /// This function runs in its own thread, concurrently with the threads for other config files
        /// </summary>
        /// <param name="configFilePath">The path for the config file that will be processed by the thread</param>
        /// <param name="config">The <paramref name="ReduceConfig">Config</paramref> object deserialized from disk file</param>
        private static void InitiateReduction(string configFilePath, ReduceConfig config) {
            Trace.WriteLine(string.Format( "Initializing processing of config file '{0}'", configFilePath));
            try {
                string working_path = Path.GetDirectoryName(configFilePath);
                string qvw_path = Path.Combine(working_path, config.MasterQVW);
                string file_name = Path.GetFileNameWithoutExtension(configFilePath), flag_file_name = string.Empty;
                File.Create(flag_file_name = Path.Combine(working_path, string.Format("{0}_running.{1}", file_name, "txt"))).Close();
                Trace.WriteLine( string.Format("Created processing flag file '{0}'", file_name));

                if(!File.Exists(qvw_path) ) {
                    Trace.WriteLine(string.Format("Config file is pointing to an unavailable QVW file '{0}'. The process will be terminated...", qvw_path));
                    return;
                } else {
                    Trace.WriteLine(string.Format("Found processable QVW file on '{0}'", qvw_path));
                }
                XMLFileSignature signature = new XMLFileSignature(qvw_path);
                bool t = false;
                if( !string.IsNullOrEmpty(signature.ErrorMessage) ) {
                    Trace.WriteLine(signature.ErrorMessage);
                    return;

                } else if( !signature.SignatureDictionary.Keys.Contains("can_emit")
                    || !bool.TryParse(signature.SignatureDictionary["can_emit"], out t)
                    || !t ) {
                    Trace.WriteLine(string.Format("File '{0}' marked to not be processed. Process will be terminated with success.", config.MasterQVW));
                    return;
                } else {
                    Trace.WriteLine( string.Format("File '{0}' is correctly signed and marked to be processed. Shipping to Loop & Reduce on the Publisher Server...", config.MasterQVW));
                    // Connect with QMSAPI, start configuring the reduction process
                    Milliman.Reduction.ReductionEngine.QMSSettings settings = new ReductionEngine.QMSSettings();
                    settings.QMSURL = System.Configuration.ConfigurationManager.AppSettings["QMS"];
                    settings.UserName = System.Configuration.ConfigurationManager.AppSettings["QMSUser"];
                    settings.Password = System.Configuration.ConfigurationManager.AppSettings["QMSPassword"];
                    Trace.WriteLine(string.Format("QMS Address is: '{0}', and QMS User is '{1}'", settings.QMSURL,settings.UserName));

                    ReductionEngine.ReductionRunner runner = new ReductionEngine.ReductionRunner(settings);
                    runner.ConfigFileContent = config;
                    runner.QVWOriginalFullFileName = Path.Combine(Path.GetDirectoryName(configFilePath), config.MasterQVW);
                    runner.Run();
                    Trace.WriteLine("Process finishing successfully.");
                }
                
            } catch(Exception ex ) {
                Trace.WriteLine(string.Format("Unable to finish processing config file '{0}'\r\n{1}\r\n{2}", configFilePath, ex.Message, ex.StackTrace));
                return;
            }
        }
        
        /// <summary>
        /// Deserializes a config file and validates its configuration
        /// </summary>
        /// <param name="workingPath">The UNC path or Local path where the config files reside</param>
        /// <returns></returns>
        private static bool  IsConfigFileOk(string workingPath) {
            Dictionary<string, ReduceConfig> config_dic = new Dictionary<string, ReduceConfig>();
            Trace.WriteLine(string.Format("Sweeping through folder '{0}'", workingPath));
            try {
                foreach(var config_file in Directory.GetFiles(workingPath, "*.config") ) {
                    try {
                        config_dic.Add(config_file, ReduceConfig.Deserialize(config_file));
                        Trace.WriteLine(string.Format("Successfully added configuration file: '{0}'", config_file));
                    }catch(Exception ex2 ) {
                        // If ONE single file fails to load, we cancel the operation for all of them
                        Trace.WriteLine(string.Format("Unable to process configuration file '{0}'. {1}\r\nAll config files from working path '{2}' will be cancelled...", config_file, ex2.Message, workingPath));
                        return false;
                    }
                }

                // Assumes all config files were read-in ok...
                if( config_dic.Count == 0 ) {
                    Trace.WriteLine("Found no valid config files on designated path.");
                    FinishProcess(Path.Combine(workingPath, "request_complete.txt"));
                    return false;
                } else {
                    Trace.WriteLine(string.Format("Adding {0} jobs to queue", config_dic.Count));
                    AddJobs(config_dic);
                }
            }catch(Exception ex ) {
                Trace.WriteLine(string.Format("Error while processing config files {0}", ex));
                return false;   
            }
            return true;
        }

        /// <summary>
        /// Creates entry in private queue for each config file, which will later spawn a thread and be 
        /// processed in parallel by the <seealso cref="InitiateReduction(string, ReduceConfig)">InitiateReduction</seealso> method
        /// </summary>
        /// <param name="configFiles"></param>
        private static void AddJobs(Dictionary<string, ReduceConfig> configFiles) {
            lock ( _LockQueue ) {
                foreach( var key in configFiles.Keys )
                    _queue.Enqueue(new KeyValuePair<string, ReduceConfig>(key, configFiles[key]));
                _eventHasJobs.Set();
            }
        }

        private static void FinishProcess(string sourceConfigFile) {
            // This function's called when a process is completed (async from a ReductionThread)
            // or when there was an error and we can't really process anything on the specified folder.
            // Either way, cleaning up is always good...
            string working_path = Path.GetDirectoryName(sourceConfigFile);
            string file_name = Path.GetFileNameWithoutExtension(sourceConfigFile);
            string flag_file_name = Path.Combine(working_path, string.Format("{0}_running", file_name));
            try {
                File.Move(Path.Combine(working_path, string.Format("{0}.txt", flag_file_name)),
                    Path.Combine(working_path, string.Format("{0}_{1}.txt", flag_file_name, DateTime.Now.ToString("yyyyMMdd_hhmmssfff"))));
                //File.Create(Path.Combine(working_path, string.Format("{0}{1}.{2}", file_name, DateTime.Now.ToString("yyyyMMdd_hhmmssfff"), file_extension), file_extension));
            }
            catch (Exception e )
            {
                Trace.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Manages semaphore files on disk that will tell the whole folder's ready to be processed
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static bool IsSemaphoresOk(string filePath) {
            try {
                Trace.WriteLine("Changing source semaphore");
                string fileNameChanged = GetFileNameTimeStamp(filePath);
                if( !File.Exists(filePath) ) return true;
                System.IO.File.Move(filePath, fileNameChanged);
                Trace.WriteLine(string.Format("FileName changed from '{0}' to '{1}'", filePath, fileNameChanged));
                Trace.WriteLine("Skipping semaphore for the running flag. Will create on a per-config basis. Semaphores are ok...");
                //File.Create(Path.Combine(Path.GetDirectoryName(filePath), "request_running.txt"));
                //_L.Info("Semaphore management complete.");
            } catch( Exception ex ) {
                Trace.WriteLine(string.Format("An error occurred while setting up file semaphores...{0}", ex));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Attempt to rename the file for at least 1 second
        /// </summary>
        /// <param name="SemaphoreFile"></param>
        /// <param name="NewName"></param>
        /// <returns></returns>
        private static bool SemaphoreRename(string SemaphoreFile, string NewName)
        {
            int Count = 0;
            while (true)
            {
                try
                {
                    System.IO.File.Move(SemaphoreFile, NewName);
                    return true;
                }
                catch (Exception ex)
                {
                    string s = ex.ToString();
                    //expensive, but works - keeps primary thread from finding file to fast when set to high rate of execution
                }
                Count++;
                if (Count > 10)
                    return false;
                System.Threading.Thread.Sleep(100);
            }
        }

        private static string GetWorkingPath(string arg) {
            string workingPath = string.Empty ;
            try {
                if( Directory.Exists(arg) ) {
                    Trace.WriteLine(string.Format("Primary argument is already a folder. Defining working path as '{0}'", arg));
                    workingPath = arg;
                } else {
                    Trace.WriteLine(string.Format("Attempting to get folder name from primary argument '{0}'", arg));
                    workingPath = Path.GetDirectoryName(arg);
                    if( !Directory.Exists(Path.GetDirectoryName(workingPath)) ) {
                        Trace.WriteLine(string.Format("Extracted folder '{0}' from argument but the folder does not exist or is not accessible. Current processing will be terminated", workingPath));
                        workingPath = string.Empty;
                    } else {
                        Trace.WriteLine(string.Format("Setting working path to '{0}'", workingPath));
                    }
                }
                return workingPath;
            } catch(Exception ex ) {
                Trace.WriteLine(string.Format("An error occurred while trying to process the working path. {0}", ex));
                return string.Empty;
            }
        }

        private static string GetFileNameTimeStamp(string fullFileName) {
            string directoryName = Path.GetDirectoryName(fullFileName);
            string fileName = Path.GetFileNameWithoutExtension(fullFileName);
            string ext = Path.GetExtension(fullFileName);
            return Path.Combine(directoryName, string.Format("{0}_{1}{2}", fileName, DateTime.Now.ToString("yyyyMMdd_hhmmssfff"), ext));
        }
    }
}

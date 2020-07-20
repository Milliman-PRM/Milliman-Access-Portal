/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MillimanAccessPortal.Models.FileDropModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Linq;

namespace MillimanAccessPortal.Services
{
    public enum FileDropUploadTaskStatus
    {
        Unknown = 0,
        Requested,
        FinalizingUpload,
        ValidatingFile,
        Copying,
        Completed,
        Error,
    }

    public class FileDropUploadTask
    {
        internal Guid FileUploadId { get; set; }

        internal FileDropUploadTaskStatus Status { get; set; } = FileDropUploadTaskStatus.Unknown;

        internal DateTime? CompletionDateTimeUtc { get; set; } = null;

        internal Guid FileDropDirectoryId { get; set; }

        internal string FileName { get; set; }

        internal FileDropUploadTask(ProcessUploadedFileModel requestModel, FileDropUploadTaskStatus status)
        {
            FileUploadId = requestModel.FileUploadId;
            FileDropDirectoryId = requestModel.FileDropDirectoryId;
            FileName = requestModel.FileName;
            Status = status;
        }
    }

    public interface IFileDropUploadTaskTracker
    {
        Guid RequestUploadProcessing(ProcessUploadedFileModel requestModel);

        FileDropUploadTask GetExistingTask(Guid taskId);

        IEnumerable<KeyValuePair<Guid, FileDropUploadTask>> GetNewTasks(TimeSpan waitTime);

        bool RemoveExistingTask(Guid taskId);

        void UpdateTaskStatus(Guid taskId, FileDropUploadTaskStatus status, DateTime? completionTimeUtc = null);
    }

    public class FileDropUploadTaskTracker : IFileDropUploadTaskTracker
    {
        private ConcurrentDictionary<Guid, FileDropUploadTask> _taskDict = new ConcurrentDictionary<Guid, FileDropUploadTask>();
        private AutoResetEvent _signal = new AutoResetEvent(false);

        public Guid RequestUploadProcessing(ProcessUploadedFileModel requestModel)
        {
            FileDropUploadTask newTask = new FileDropUploadTask(requestModel, FileDropUploadTaskStatus.Requested);

            _taskDict.AddOrUpdate(requestModel.FileUploadId, newTask, /*impossible*/ (k, v) => throw new ApplicationException("Key already exists while adding FileDropUploadTask to ConcurrentDictionary"));
            _signal.Set();

            return requestModel.FileUploadId;
        }

        /// <summary>
        /// Waits up to the specified time for the signal event to be set and returns whatever dictionary elements have Requested status
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<Guid, FileDropUploadTask>> GetNewTasks(TimeSpan waitTime)
        {
            _signal.WaitOne(waitTime);

            foreach (var newTaskKvp in _taskDict.Where(t => t.Value.Status == FileDropUploadTaskStatus.Requested))
            {
                newTaskKvp.Value.Status = FileDropUploadTaskStatus.FinalizingUpload;
                yield return newTaskKvp;
            }
        }

        public void UpdateTaskStatus(Guid taskId, FileDropUploadTaskStatus status, DateTime? completionTimeUtc = null)
        {
            if (! _taskDict.TryGetValue(taskId, out FileDropUploadTask existingItem))
            {
                return;
            }

            existingItem.Status = status;
            existingItem.CompletionDateTimeUtc = completionTimeUtc;
        }

        public bool RemoveExistingTask(Guid taskId)
        {
            return _taskDict.TryRemove(taskId, out _);
        }

        public FileDropUploadTask GetExistingTask(Guid taskId)
        {
            _taskDict.TryGetValue(taskId, out FileDropUploadTask task);
            return task;
        }
    }
}

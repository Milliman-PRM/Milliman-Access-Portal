/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib;
using MapDbContextLib.Context;
using Microsoft.Extensions.DependencyInjection;
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
        CompletedRenamed,
    }

    public class FileDropUploadTask
    {
        internal Guid FileUploadId { get; set; }

        internal FileDropUploadTaskStatus Status { get; set; } = FileDropUploadTaskStatus.Unknown;

        internal DateTime? CompletionDateTimeUtc { get; set; } = null;

        internal Guid FileDropDirectoryId { get; set; }

        internal string FileName { get; set; }

        internal SftpAccount Account { get; set; }

        internal FileDropUploadTask(ProcessUploadedFileModel requestModel, FileDropUploadTaskStatus status, SftpAccount account)
        {
            FileUploadId = requestModel.FileUploadId;
            FileDropDirectoryId = requestModel.FileDropDirectoryId;
            FileName = requestModel.FileName;
            Status = status;
            Account = account;
        }
    }

    public interface IFileDropUploadTaskTracker
    {
        Guid RequestUploadProcessing(ProcessUploadedFileModel requestModel, SftpAccount requestingAccount);

        FileDropUploadTask GetExistingTask(Guid taskId);

        IEnumerable<KeyValuePair<Guid, FileDropUploadTask>> GetNewTasks(TimeSpan waitTime);

        bool RemoveExistingTask(Guid taskId);

        void UpdateTaskStatus(Guid taskId, FileDropUploadTaskStatus status, DateTime? completionTimeUtc = null);

        Task<bool> RemoveFileUploadAsync(Guid fileUploadId);
        Task<bool> RemoveFileUploadAsync(FileUpload fileUploadRecord);
    }

    public class FileDropUploadTaskTracker : IFileDropUploadTaskTracker
    {
        private ConcurrentDictionary<Guid, FileDropUploadTask> _taskDict = new ConcurrentDictionary<Guid, FileDropUploadTask>();
        private AutoResetEvent _signal = new AutoResetEvent(false);
        private readonly IServiceProvider _services;

        public FileDropUploadTaskTracker(
            IServiceProvider servicesArg)
        {
            _services = servicesArg;
        }

        public Guid RequestUploadProcessing(ProcessUploadedFileModel requestModel, SftpAccount requestingAccount)
        {
            FileDropUploadTask newTask = new FileDropUploadTask(requestModel, FileDropUploadTaskStatus.Requested, requestingAccount);

            _taskDict.AddOrUpdate(requestModel.FileUploadId, newTask, (k, v) => throw new ApplicationException("Key already exists while adding FileDropUploadTask to ConcurrentDictionary"));
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

        public async Task<bool> RemoveFileUploadAsync(Guid fileUploadId)
        {
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var record = await dbContext.FileUpload.FindAsync(fileUploadId);
                return await RemoveFileUploadAsync(record);
            }
        }

        public async Task<bool> RemoveFileUploadAsync(FileUpload fileUploadRecord)
        {
            if (fileUploadRecord == null || fileUploadRecord.Status == FileUploadStatus.InProgress)
            {
                return false;
            }

            try
            {
                using (var scope = _services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    dbContext.FileUpload.Remove(fileUploadRecord);
                    FileSystemUtil.DeleteFileWithRetry(fileUploadRecord.StoragePath);
                    await dbContext.SaveChangesAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

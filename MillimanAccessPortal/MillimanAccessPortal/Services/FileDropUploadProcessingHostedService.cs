/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Asynchronous handling of FileDrop file uploads
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib;
using MapDbContextLib.Context;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Serilog;
using MillimanAccessPortal.Controllers;

namespace MillimanAccessPortal.Services
{
    public class FileDropUploadProcessingHostedService : BackgroundService
    {
        protected ConcurrentDictionary<Guid, Task> _runningTasks = new ConcurrentDictionary<Guid, Task>();
        private readonly IFileDropUploadTaskTracker _fileDropUploadTaskTracker;
        private readonly IServiceProvider _services;

        public FileDropUploadProcessingHostedService(
            IFileDropUploadTaskTracker fileDropUploadTaskTrackerArg,
            IServiceProvider servicesArg)
        {
            _fileDropUploadTaskTracker = fileDropUploadTaskTrackerArg;
            _services = servicesArg;
        }

        protected override Task ExecuteAsync(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                // Clean up completed items
                foreach (var completedKvp in _runningTasks.Where(kvp => kvp.Value.Status == TaskStatus.RanToCompletion).ToList())
                {
                    if (completedKvp.Value.IsFaulted)
                    {
                        _fileDropUploadTaskTracker.UpdateTaskStatus(completedKvp.Key, FileDropUploadTaskStatus.Error, DateTime.UtcNow);
                    }

                    _runningTasks.TryRemove(completedKvp.Key, out _);
                }

                foreach (KeyValuePair<Guid, FileDropUploadTask> taskKvp in _fileDropUploadTaskTracker.GetNewTasks(TimeSpan.FromSeconds(10)))
                {
                    Task processingTask = ProcessOneUploadAsync(taskKvp);
                    _runningTasks.TryAdd(taskKvp.Key, processingTask);
                }
            }

            return Task.CompletedTask;
        }

        private async Task ProcessOneUploadAsync(KeyValuePair<Guid, FileDropUploadTask> taskKvp)
        {
            while (taskKvp.Value.Status == FileDropUploadTaskStatus.ValidatingFile)
            {

            }

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var uploadRecord = await dbContext.FileUpload.FindAsync(taskKvp.Value.FileUploadId);

                // Wait for uploaded file chunks to be reassembled
                while (uploadRecord.Status == FileUploadStatus.InProgress && DateTime.UtcNow - uploadRecord.InitiatedDateTimeUtc < TimeSpan.FromSeconds(60))
                {
                    dbContext.Entry(uploadRecord).State = EntityState.Detached;
                    await Task.Delay(2000);
                    uploadRecord = await dbContext.FileUpload.FindAsync(taskKvp.Value.FileUploadId);
                }

                if (uploadRecord.Status != FileUploadStatus.Complete)
                {
                    _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.Error);
                    await RemoveFileUpload(uploadRecord);

                    return;
                }

                _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.ValidatingFile);

                while (!uploadRecord.VirusScanWindowComplete)
                {
                    dbContext.Entry(uploadRecord).State = EntityState.Detached;
                    await Task.Delay(2000);
                    uploadRecord = await dbContext.FileUpload.FindAsync(taskKvp.Value.FileUploadId);
                }

                var destinationDirectoryRecord = await dbContext.FileDropDirectory
                                                                .Include(d => d.FileDrop)
                                                                .SingleOrDefaultAsync(d => d.Id == taskKvp.Value.FileDropDirectoryId);

                string targetFullPath = Path.Combine(destinationDirectoryRecord.FileDrop.RootPath, 
                                                     destinationDirectoryRecord.CanonicalFileDropPath.TrimStart('/'), 
                                                     taskKvp.Value.FileName);

                _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.Copying);

                try
                {
                    FileSystemUtil.CopyFileWithRetry(uploadRecord.StoragePath, targetFullPath, false);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, $"ProcessOneUploadAsync failed to copy uploaded file from {uploadRecord.StoragePath} to {targetFullPath}");
                    _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.Error);
                    return;
                }

                bool removeUploadSuccess = await RemoveFileUpload(uploadRecord);

                if (removeUploadSuccess)
                {
                    _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.Completed);
                }
            }

        }

        private async Task<bool> RemoveFileUpload(FileUpload fileUploadRecord)
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

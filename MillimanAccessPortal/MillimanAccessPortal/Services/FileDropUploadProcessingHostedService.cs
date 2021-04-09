/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Asynchronous handling of FileDrop file uploads
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib.Event;
using AuditLogLib.Models;
using AuditLogLib.Services;
using MapCommonLib;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using FileDropLib;
using MapDbContextLib.Models;

namespace MillimanAccessPortal.Services
{
    public class FileDropUploadProcessingHostedService : BackgroundService
    {
        protected ConcurrentDictionary<Guid, Task> _runningTasks = new ConcurrentDictionary<Guid, Task>();
        private readonly IFileDropUploadTaskTracker _fileDropUploadTaskTracker;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _appConfig;

        public FileDropUploadProcessingHostedService(
            IFileDropUploadTaskTracker fileDropUploadTaskTrackerArg,
            IServiceProvider servicesArg,
            IConfiguration configArg)
        {
            _fileDropUploadTaskTracker = fileDropUploadTaskTrackerArg;
            _services = servicesArg;
            _appConfig = configArg;
        }

        protected async override Task ExecuteAsync(CancellationToken cancelToken)
        {
            await Task.Yield();

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
        }

        private async Task ProcessOneUploadAsync(KeyValuePair<Guid, FileDropUploadTask> taskKvp)
        {
            DateTime stopWaitingAtUtc = DateTime.UtcNow + TimeSpan.FromSeconds(60);

            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();

                FileUpload uploadRecord = await dbContext.FileUpload.FindAsync(taskKvp.Value.FileUploadId);

                Log.Debug("FileDrop upload task transitioning to FinalizingUpload status");
                _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.FinalizingUpload);
                while (uploadRecord.Status == FileUploadStatus.InProgress)
                {
                    if (DateTime.UtcNow > stopWaitingAtUtc)
                    {
                        _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.Error);
                        await _fileDropUploadTaskTracker.RemoveFileUploadAsync(taskKvp.Key);
                        return;
                    }
                    await Task.Delay(1000);
                    dbContext.Entry(uploadRecord).State = EntityState.Detached;
                    uploadRecord = await dbContext.FileUpload.FindAsync(taskKvp.Value.FileUploadId);
                }

                Log.Debug("FileDrop upload task transitioning to ValidatingFile status");
                _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.ValidatingFile);
                stopWaitingAtUtc = DateTime.UtcNow + TimeSpan.FromSeconds(60);
                while (!uploadRecord.VirusScanWindowComplete)
                {
                    if (DateTime.UtcNow > stopWaitingAtUtc)
                    {
                        _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.Error);
                        await _fileDropUploadTaskTracker.RemoveFileUploadAsync(taskKvp.Key);
                        return;
                    }
                    await Task.Delay(1000);
                    dbContext.Entry(uploadRecord).State = EntityState.Detached;
                    uploadRecord = await dbContext.FileUpload.FindAsync(taskKvp.Value.FileUploadId);
                }

                Log.Debug("FileDrop upload task transitioning to Copying status");
                _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.Copying);

                var destinationDirectoryRecord = await dbContext.FileDropDirectory
                                                                .Include(d => d.FileDrop)
                                                                .Include(d => d.Files)
                                                                .SingleOrDefaultAsync(d => d.Id == taskKvp.Value.FileDropDirectoryId);

                bool fileRenamed = false;
                while (destinationDirectoryRecord.Files.Select(f => f.FileName).Contains(taskKvp.Value.FileName, StringComparer.InvariantCultureIgnoreCase))
                {
                    // requested file name already exists
                    string requestedNameSansExtension = Path.GetFileNameWithoutExtension(taskKvp.Value.FileName);
                    string requestedExtension = Path.GetExtension(taskKvp.Value.FileName);

                    Regex regex = new Regex(@" \((\d+)\)$");  // search for " (#)" at the end
                    Match match = regex.Match(requestedNameSansExtension);
                    if (match.Success)
                    {
                        int newNumber = int.Parse(match.Groups[1].Value) + 1;
                        string replacement = match.Value.Replace(match.Groups[1].Value, newNumber.ToString());
                        taskKvp.Value.FileName = taskKvp.Value.FileName.Replace(match.Value, replacement);
                    }
                    else
                    {
                        taskKvp.Value.FileName = requestedNameSansExtension + " (1)" + requestedExtension;
                    }

                    fileRenamed = true;
                }

                string targetFullPath = Path.Combine(_appConfig.GetValue<string>("Storage:FileDropRoot"), 
                                                     destinationDirectoryRecord.FileDrop.RootPath, 
                                                     destinationDirectoryRecord.CanonicalFileDropPath.TrimStart('/'), 
                                                     taskKvp.Value.FileName);

                try
                {
                    FileSystemUtil.CopyFileWithRetry(uploadRecord.StoragePath, targetFullPath, false);
                    FileInfo fi = new FileInfo(targetFullPath);
                    FileDropFile newFileRecord = new FileDropFile
                    {
                        FileName = taskKvp.Value.FileName,
                        DirectoryId = destinationDirectoryRecord.Id,
                        CreatedByAccountUserName = taskKvp.Value.Account.UserName,
                        Size = fi.Length,
                        UploadDateTimeUtc = fi.LastWriteTimeUtc,
                    };

                    dbContext.FileDropFile.Add(newFileRecord);
                    await dbContext.SaveChangesAsync();

                    FileDropOperations.HandleUserNotifications(
                        destinationDirectoryRecord.FileDrop.Id,
                        destinationDirectoryRecord.FileDrop.Name,
                        Path.Combine(destinationDirectoryRecord.CanonicalFileDropPath, taskKvp.Value.FileName).Replace('\\', '/'),
                        FileDropNotificationType.FileWrite
                    );                    

                    Log.Information($"ProcessOneUploadAsync success processing uploaded file from {uploadRecord.StoragePath} to {targetFullPath}");
                    auditLogger.Log(AuditEventType.SftpFileWriteAuthorized.ToEvent(new SftpFileOperationLogModel
                    {
                        FileName = taskKvp.Value.FileName,
                        FileDropDirectory = (FileDropDirectoryLogModel)destinationDirectoryRecord,
                        FileDrop = new FileDropLogModel { Id = destinationDirectoryRecord.FileDrop.Id, Name = destinationDirectoryRecord.FileDrop.Name, RootPath = Path.Combine(_appConfig.GetValue<string>("Storage:FileDropRoot"), destinationDirectoryRecord.FileDrop.RootPath) },
                        Account = taskKvp.Value.Account,
                        User = taskKvp.Value.Account.ApplicationUser,
                    }), taskKvp.Value.Account.ApplicationUser.UserName, taskKvp.Value.Account.ApplicationUser.Id);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, $"ProcessOneUploadAsync failed to copy uploaded file from {uploadRecord.StoragePath} to {targetFullPath}");
                    _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.Error);
                    return;
                }

                bool removeUploadSuccess = await _fileDropUploadTaskTracker.RemoveFileUploadAsync(uploadRecord);

                if (removeUploadSuccess)
                {
                    if (fileRenamed)
                    {
                        _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.CompletedRenamed);
                    }
                    else
                    {
                        _fileDropUploadTaskTracker.UpdateTaskStatus(taskKvp.Key, FileDropUploadTaskStatus.Completed);
                    }
                }
            }

        }
    }
}

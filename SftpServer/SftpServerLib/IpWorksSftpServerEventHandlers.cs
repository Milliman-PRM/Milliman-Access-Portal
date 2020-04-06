/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using AuditLogLib.Models;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using nsoftware.IPWorksSSH;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SftpServerLib
{
    internal class IpWorksSftpServerEventHandlers
    {
        internal static JsonSerializerOptions prettyJsonOptions = new JsonSerializerOptions { WriteIndented = true, };

        #region Event handlers
        //[Description("Information about errors during data delivery.")]
        internal static void OnError(object sender, SftpserverErrorEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("Error", evtData));
        }

        //[Description("Fires when a client wants to delete a file.")]
        internal static void OnFileRemove(object sender, SftpserverFileRemoveEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("FileRemove", evtData));
        }

        //[Description("Fires when a client wants to read from an open file.")]
        internal static void OnFileRead(object sender, SftpserverFileReadEventArgs evtData)
        {
            // This event occurs between OnFileOpen and OnFileClose, only to document transfer of a block of file data
            Log.Information(GenerateEventArgsLogMessage("FileRead", evtData));
        }

        //[Description("Fires when a client wants to open or create a file.")]
        internal static void OnFileOpen(object sender, SftpserverFileOpenEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("FileOpen", evtData));

            // Documentation for this event is at http://cdn.nsoftware.com/help/IHF/cs/SFTPServer_e_FileOpen.htm

            RequiredAccess requiredAccess = default;
            switch (evtData.Flags)
            {
                case int flags when (flags & 0x00000002) == 0x00000002:  // SSH_FXF_WRITE (0x00000002)
                    requiredAccess = RequiredAccess.Write;
                    break;

                case int flags when (flags & 0x00000001) == 0x00000001:  // SSH_FXF_READ (0x00000001)
                    requiredAccess = RequiredAccess.Read;
                    break;

                default:
                    Log.Warning($"OnFileOpen event invoked for unsupported event data flags value {evtData.Flags:X8}");
                    evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                    return;
            }

            (AuthorizationResult authResult, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, requiredAccess);
            if (authResult == AuthorizationResult.ConnectionNotFound)
            {
                Log.Warning($"OnFileOpen event invoked but requested connection id {evtData.ConnectionId} was not found for account {evtData.User}");
                evtData.StatusCode = 7;  // SSH_FX_CONNECTION_LOST 7
                return;
            }

            connection.LastActivityUtc = DateTime.UtcNow;

            if (evtData.BeforeExec)
            {
                if (authResult == AuthorizationResult.NotAuthorized)
                {
                    Log.Information($"OnFileOpen event invoked for file write, but write access is denied for account {evtData.User}");
                    evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                    return;
                }

                string absoluteFilePath = Path.Combine(connection.FileDropRootPathAbsolute, evtData.Path.TrimStart('/', '\\'));
                string dirPathToFind = FileDropDirectory.ConvertPathToCanonicalPath(Path.GetDirectoryName(evtData.Path));
                string fileName = Path.GetFileName(evtData.Path);

                FileDropDirectory containingDirectory = default;
                using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
                {
                    containingDirectory = db.FileDropDirectory
                                            .Where(d => d.FileDropId == connection.FileDropId)
                                            .SingleOrDefault(d => EF.Functions.ILike(dirPathToFind, d.CanonicalFileDropPath));
                }

                if (containingDirectory == null)
                {
                    Log.Warning($"OnFileOpen event invoked for file write, but containing directory {dirPathToFind} database record was not foundc");
                    evtData.StatusCode = 10;  // SSH_FX_NO_SUCH_PATH 10
                    return;
                }

                switch (evtData.Flags)
                {
                    case int flags when (flags & 0x00000008) == 0x00000008  // SSH_FXF_CREAT (0x00000008)
                                     && (flags & 0x00000002) == 0x00000002  // SSH_FXF_WRITE (0x00000002)
                                     && evtData.FileType == 1:   // SSH_FILEXFER_TYPE_REGULAR(1)
                        // create/write a file

                        using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
                        {
                            if ((evtData.Flags & 0x00000020) != 0x00000020 &&  // SSH_FXF_EXCL  (0x00000020)
                                File.Exists(absoluteFilePath))
                            {
                                if (GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.Delete).result != AuthorizationResult.Authorized)
                                {
                                    Log.Warning($"OnFileOpen event invoked for file write to {absoluteFilePath}, but file already exists and delete access is denied, account {evtData.User}");
                                    evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                                    return;
                                }

                                var existingFileRecord = db.FileDropFile
                                                           .SingleOrDefault(f => f.FileName == fileName
                                                                              && f.DirectoryId == containingDirectory.Id);
                                if (existingFileRecord != null)
                                {
                                    db.FileDropFile.Remove(existingFileRecord);
                                    File.Delete(absoluteFilePath);
                                    db.SaveChanges();
                                }
                            }

                            db.FileDropFile.Add(new FileDropFile
                            {
                                FileName = fileName,
                                DirectoryId = containingDirectory.Id,
                                CreatedByAccountId = connection.Account.Id,
                            });
                            db.SaveChanges();

                            new AuditLogger().Log(AuditEventType.SftpFileWriteAuthorized.ToEvent(
                                new SftpFileOperationLogModel
                                {
                                    FileName = fileName,
                                    FileDropDirectory = (FileDropDirectoryLogModel)containingDirectory,
                                    FileDrop = new FileDropLogModel { Id = connection.FileDropId.Value, Name = connection.FileDropName, RootPath = connection.FileDropRootPathAbsolute },
                                    Account = connection.Account,
                                    User = connection.MapUser,
                                }
                            ), connection.MapUser?.UserName);
                            Log.Information($"File {evtData.Path} authorized for writing, user {evtData.User}");
                        }
                        break;

                    case int flags when (flags & 0x00000001) == 0x00000001  // SSH_FXF_READ (0x00000001)
                                     && evtData.FileType == 1:   // SSH_FILEXFER_TYPE_REGULAR(1)
                        // read a file

                        using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
                        {
                            if (!db.FileDropFile.Any(f => EF.Functions.ILike(f.FileName, fileName)) ||
                                !File.Exists(absoluteFilePath))
                            {
                                Log.Warning($"OnFileOpen event invoked for file read, but file {evtData.Path} or file record not found, account {evtData.User}");
                                evtData.StatusCode = 4;  // SSH_FX_FAILURE 4
                                return;
                            }

                            new AuditLogger().Log(AuditEventType.SftpFileReadAuthorized.ToEvent(
                                new SftpFileOperationLogModel
                                {
                                    FileName = fileName,
                                    FileDropDirectory = (FileDropDirectoryLogModel)containingDirectory,
                                    FileDrop = new FileDropLogModel { Id = connection.FileDropId.Value, Name = connection.FileDropName, RootPath = connection.FileDropRootPathAbsolute },
                                    Account = connection.Account,
                                    User = connection.MapUser,
                                }
                            ), connection.MapUser?.UserName);
                            Log.Information($"File {evtData.Path} authorized for reading, user {evtData.User}");
                        }
                        break;

                    default:
                        evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                        return;
                }
            }
        }

        //[Description("Fires when a client attempts to close an open file or directory handle.")]
        internal static void OnFileClose(object sender, SftpserverFileCloseEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("FileClose", evtData));
        }

        //[Description("Fired when a connection is closed.")]
        internal static void OnDisconnected(object sender, SftpserverDisconnectedEventArgs evtData)
        {
            if (IpWorksSftpServer._connections.Keys.Contains(evtData.ConnectionId))
            {
                var connectionProperties = IpWorksSftpServer._connections[evtData.ConnectionId];
                Log.Information($"Connection <{evtData.ConnectionId}> close requested for account name {connectionProperties.Account?.UserName}");
                IpWorksSftpServer._connections.Remove(evtData.ConnectionId);
            }
            Log.Information($"Connection <{evtData.ConnectionId}> closed");
        }

        //[Description("Fires when a client wants to delete a directory.")]
        internal static void OnDirRemove(object sender, SftpserverDirRemoveEventArgs evtData)
        {
            // IpWorks documentation for this event at http://cdn.nsoftware.com/help/IHF/cs/SFTPServer_e_DirRemove.htm

            (AuthorizationResult result, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.Delete);

            if (result == AuthorizationResult.ConnectionNotFound)
            {
                Log.Warning("OnDirRemove: Connection not found, eventdata: {@evtData}", evtData);
                evtData.StatusCode = 7;  // SSH_FX_CONNECTION_LOST 7
                return;
            }

            connection.LastActivityUtc = DateTime.UtcNow;

            if (result == AuthorizationResult.NotAuthorized)
            {
                Log.Warning("OnDirRemove: Permission denied, eventdata: {@evtData}", evtData);
                evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                return;
            }

            string requestedCanonicalPath = FileDropDirectory.ConvertPathToCanonicalPath(evtData.Path);
            if (string.IsNullOrWhiteSpace(requestedCanonicalPath) || requestedCanonicalPath == "/")
            {
                Log.Warning($"OnDirRemove: Invalid path requested: <{evtData.Path}>");
                evtData.StatusCode = 8;  // SSH_FX_OP_UNSUPPORTED 8
                return;
            }
            string requestedAbsolutePath = Path.Combine(connection.FileDropRootPathAbsolute, evtData.Path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            using (var db = GlobalResources.NewMapDbContext)
            {
                List<FileDropDirectory> allDirectoryRecordsForFileDrop = db.FileDropDirectory.Where(d => d.FileDropId == connection.FileDropId).ToList();
                FileDropDirectory requestedDirectory = allDirectoryRecordsForFileDrop.SingleOrDefault(d => d.CanonicalFileDropPath == evtData.Path);

                if (evtData.BeforeExec)
                {
                    if (!Directory.Exists(requestedAbsolutePath) || requestedDirectory == null)
                    {
                        Log.Warning($"OnDirRemove: Requested directory {evtData.Path} at absolute path {requestedAbsolutePath} is not found (database or file system)");
                        evtData.StatusCode = 10;  // SSH_FX_NO_SUCH_PATH 10
                        return;
                    }
                }
                else
                {
                    List<FileDropDirectory> directoriesToDelete = allDirectoryRecordsForFileDrop.Where(d => EF.Functions.Like(d.CanonicalFileDropPath, requestedCanonicalPath + "%")).ToList();

                    var deleteInventory = new FileDropDirectoryInventoryModel
                    {
                        Directories = directoriesToDelete.Select(d => (FileDropDirectoryLogModel)d).ToList(),
                        Files = db.FileDropFile
                                  .Where(f => directoriesToDelete.Select(d => d.Id).Contains(f.DirectoryId))
                                  .ToList(),
                    };

                    // Cascade on delete behavior will remove all subfolder and contained file records
                    db.FileDropDirectory.Remove(requestedDirectory);  // This should cascade to all child directory and contained file records
                    db.SaveChanges();

                    new AuditLogger().Log(AuditEventType.SftpDirectoryRemoved.ToEvent((FileDropDirectoryLogModel)requestedDirectory, 
                                                                                      deleteInventory, 
                                                                                      new FileDropLogModel { Id = connection.FileDropId.Value, Name = connection.FileDropName }, 
                                                                                      connection.Account,
                                                                                      connection.MapUser));
                    Log.Information($"OnDirRemove: Requested directory {evtData.Path} at absolute path {requestedAbsolutePath} removed. Deleted inventory is {{@Inventory}}", deleteInventory);
                }
            }
        }

        //[Description("Fires when a client attempts to open a directory for listing.")]
        internal static void OnDirList(object sender, SftpserverDirListEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("DirList", evtData));
        }

        internal static object OnDirCreateLockObj = new object();
        //[Description("Fires when a client wants to create a new directory.")]
        internal static void OnDirCreate(object sender, SftpserverDirCreateEventArgs evtData)
        {
            // IpWorks documentation for this event at http://cdn.nsoftware.com/help/IHF/cs/SFTPServer_e_DirCreate.htm
            (AuthorizationResult result, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.Write);

            if (result == AuthorizationResult.ConnectionNotFound)
            {
                Log.Warning($"OnDirCreate event invoked but no active connection was found");
                evtData.StatusCode = 7;  // SSH_FX_CONNECTION_LOST 7
                return;
            }

            connection.LastActivityUtc = DateTime.UtcNow;

            if (result == AuthorizationResult.NotAuthorized)
            {
                Log.Information($"OnDirCreate event invoked but account <{connection.Account?.Id}, {connection.Account?.UserName}> does not have Write access");
                evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                return;
            }

            string requestedCanonicalPath = FileDropDirectory.ConvertPathToCanonicalPath(evtData.Path);
            if (string.IsNullOrWhiteSpace(requestedCanonicalPath))
            {
                Log.Information($"OnDirCreate event invoked but requested path <{evtData.Path}> is invalid");
                evtData.StatusCode = 8;  // SSH_FX_OP_UNSUPPORTED 8
                return;
            }
            string requestedAbsolutePath = Path.Combine(connection.FileDropRootPathAbsolute, evtData.Path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string parentCanonicalPath = FileDropDirectory.ConvertPathToCanonicalPath(Path.GetDirectoryName(requestedCanonicalPath));
            string parentAbsolutePath = Path.Combine(connection.FileDropRootPathAbsolute, parentCanonicalPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            lock (OnDirCreateLockObj)
            {
                if (evtData.BeforeExec)
                {
                    // Validation:
                    using (var db = GlobalResources.NewMapDbContext)
                    {
                        // 1. Requested directory must not already exist
                        if (Directory.Exists(requestedAbsolutePath) ||
                            db.FileDropDirectory.Any(d => d.FileDropId == connection.FileDropId
                                                       && EF.Functions.ILike(requestedCanonicalPath, d.CanonicalFileDropPath)))
                        {
                            Log.Warning($"OnDirCreate event invoked but requested path <{evtData.Path}> already exists");
                            evtData.StatusCode = 11;  // SSH_FX_FILE_ALREADY_EXISTS 11
                            return;
                        }

                        // 2. The parent of the requested directory must already exist, including in the database
                        if (!Directory.Exists(parentAbsolutePath) ||
                            (!db.FileDropDirectory.Any(d => d.FileDropId == connection.FileDropId && EF.Functions.ILike(parentCanonicalPath, d.CanonicalFileDropPath))))
                        {
                            Log.Warning($"OnDirCreate event invoked but parent of requested path <{evtData.Path}> does not exist");
                            evtData.StatusCode = 10;  // SSH_FX_NO_SUCH_PATH 10
                            return;
                        }
                    }
                }
                else // after event has been processed
                { 
                    using (var db = GlobalResources.NewMapDbContext)
                    {
                        // Validation:
                        if (!Directory.Exists(requestedAbsolutePath))
                        {
                            Log.Warning($"OnDirCreate event invoked, the requested directory does not appear to have been created");
                            evtData.StatusCode = 4;  // SSH_FX_FAILURE 4
                            return;
                        }

                        FileDropDirectory parentRecord = db.FileDropDirectory.SingleOrDefault(d => d.FileDropId == connection.FileDropId && EF.Functions.ILike(parentCanonicalPath, d.CanonicalFileDropPath));
                        FileDropDirectory newDirRecord = new FileDropDirectory
                        {
                            CanonicalFileDropPath = requestedCanonicalPath,
                            FileDropId = connection.FileDropId.Value,
                            ParentDirectoryId = parentRecord?.Id,
                            Description = string.Empty,
                        };
                        db.FileDropDirectory.Add(newDirRecord);
                        db.SaveChanges();

                        new AuditLogger().Log(AuditEventType.SftpDirectoryCreated.ToEvent(newDirRecord,
                                                                                          new FileDropLogModel { Id = connection.FileDropId.Value, Name = connection.FileDropName },
                                                                                          connection.Account,
                                                                                          new Client { Id = connection.ClientId.Value, Name = connection.ClientName },
                                                                                          connection.MapUser));
                        Log.Information($"DirCreate event invoked, directory {requestedCanonicalPath} created relative to UserRootDirectory { IpWorksSftpServer._sftpServer.Config($"UserRootDirectory[{connection.Id}]")}");
                    }
                }
            }
        }

        //[Description("Fired when a request for connection comes from a remote host.")]
        internal static void OnConnectionRequest(object sender, SftpserverConnectionRequestEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("ConnectionRequest", evtData));
        }

        //[Description("Fired immediately after a connection completes (or fails).")]
        internal static void OnConnected(object sender, SftpserverConnectedEventArgs evtData)
        {
            DateTime now = DateTime.UtcNow;
            SftpConnectionProperties newConnection = new SftpConnectionProperties
            {
                Id = evtData.ConnectionId,
                OpenedDateTimeUtc = now,
                LastActivityUtc = now,
            };

            IpWorksSftpServer._connections[evtData.ConnectionId] = newConnection;

            Log.Information($"New connection <{evtData.ConnectionId}> accepted at {now.ToString("u")}");
        }

        //[Description("Fires when a client needs to get file information.")]
        internal static void OnGetAttributes(object sender, SftpserverGetAttributesEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("GetAttributes", evtData));

            switch (evtData.FileType)
            {
                case (int) SFTPFileTypes.sftDirectory:
                    break;

                case (int) SFTPFileTypes.sftRegular:
                    break;
            }
        }

        //[Description("Fires once for each log message.")]
        internal static void OnLog(object sender, SftpserverLogEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("Log", evtData));
            Log.Information($"Event OnLog: Args: {JsonSerializer.Serialize(evtData, prettyJsonOptions)}");
        }

        //[Description("Fires when a client attempts to canonicalize a path.")]
        internal static void OnResolvePath(object sender, SftpserverResolvePathEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("ResolvePath", evtData));
        }

        //[Description("Fires when a client wants to rename a file.")]
        internal static void OnFileRename(object sender, SftpserverFileRenameEventArgs evtData)
        {
            // Documentation for this event is at http://cdn.nsoftware.com/help/IHF/cs/SFTPServer_e_FileRename.htm

            (AuthorizationResult result, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.Write);

            if (result == AuthorizationResult.ConnectionNotFound)
            {
                Log.Warning($"OnFileRename event invoked but the requested connection was not found");
                evtData.StatusCode = 7;  // SSH_FX_CONNECTION_LOST 7
                return;
            }

            connection.LastActivityUtc = DateTime.UtcNow;

            if (result == AuthorizationResult.NotAuthorized)
            {
                Log.Information($"OnFileRename event invoked but account <{connection.Account?.Id}, {connection.Account?.UserName}> does not have Write access");
                evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                return;
            }


            if (evtData.BeforeExec)
            {
                FileAttributes attributes = File.GetAttributes(evtData.NewPath);

                using (var db = GlobalResources.NewMapDbContext)
                {
                    bool sourceRecordFound = false;
                    string recordNameString = string.Empty;

                    switch (attributes)
                    {
                        // renamed a directory
                        case FileAttributes a when (a & FileAttributes.Directory) == FileAttributes.Directory:
                            recordNameString = FileDropDirectory.ConvertPathToCanonicalPath(Path.GetFullPath(evtData.Path).Replace(Path.GetFullPath(connection.FileDropRootPathAbsolute), ""));
                            if (recordNameString == "/")
                            {
                                Log.Warning($"Request to rename {recordNameString} in FileDrop <{connection.FileDropName}> (Id {connection.FileDropId}) cannot be performed.  Root directory cannot be renamed.  Account {connection.Account?.UserName} (Id {connection.Account?.Id})");
                                evtData.StatusCode = 4;  // SSH_FX_FAILURE 4
                                return;
                            }

                            sourceRecordFound = db.FileDropDirectory.Any(d => d.FileDropId == connection.FileDropId && EF.Functions.ILike(d.CanonicalFileDropPath, recordNameString));
                            break;

                        default:
                            recordNameString = Path.GetFileName(evtData.Path);
                            sourceRecordFound = db.FileDropFile.Any(f => f.Directory.FileDropId == connection.FileDropId && EF.Functions.ILike(f.FileName, recordNameString));
                            break;
                    }

                    // confirm db connectivity and that the source record exists in the db
                    if (!sourceRecordFound)
                    {
                        Log.Warning($"Request to rename {recordNameString} in FileDrop <{connection.FileDropName}> (Id {connection.FileDropId}) cannot be performed.  Corresponding database record not found.  Account {connection.Account?.UserName} (Id {connection.Account?.Id})");
                        evtData.StatusCode = 4;  // SSH_FX_FAILURE 4
                        return;
                    }
                }
            }
            else
            {
                if (evtData.StatusCode == 0)
                {
                    string absoluteNewPath = Path.Combine(connection.FileDropRootPathAbsolute, evtData.NewPath.TrimStart('/', '\\'));
                    FileAttributes attributes = File.GetAttributes(absoluteNewPath);

                    using (var db = GlobalResources.NewMapDbContext)
                    {
                        switch (attributes)
                        {
                            // renamed a directory
                            case FileAttributes a when (a & FileAttributes.Directory) == FileAttributes.Directory:
                                List<FileDropDirectory> allDirectoriesInThisFileDrop = db.FileDropDirectory
                                                                                         .Where(d => d.FileDropId == connection.FileDropId)
                                                                                         .Include(d => d.ParentDirectory)
                                                                                         .Include(d => d.ChildDirectories)
                                                                                         .ToList();

                                FileDropDirectory directoryRecord = allDirectoriesInThisFileDrop.SingleOrDefault(d => EF.Functions.ILike(d.CanonicalFileDropPath, evtData.Path));
                                if (directoryRecord == null)
                                {
                                    evtData.StatusCode = 4;  // SSH_FX_FAILURE 4
                                    return;
                                }

                                RepathDirectoryRecord(directoryRecord, evtData.NewPath, true);

                                string newCanonicalParentPath = FileDropDirectory.ConvertPathToCanonicalPath(Path.GetDirectoryName(evtData.NewPath));
                                if (!newCanonicalParentPath.Equals(directoryRecord.ParentDirectory.CanonicalFileDropPath, StringComparison.InvariantCultureIgnoreCase))
                                { // This move involves a change in parent directory
                                    FileDropDirectory newParentDirectoryRecord = allDirectoriesInThisFileDrop
                                                                                   .Where(d => d.FileDropId == connection.FileDropId)
                                                                                   .SingleOrDefault(d => EF.Functions.ILike(d.CanonicalFileDropPath, newCanonicalParentPath));
                                    if (newParentDirectoryRecord == null)
                                    {
                                        evtData.StatusCode = 4;  // SSH_FX_FAILURE 4
                                        return;
                                    }

                                    directoryRecord.ParentDirectoryId = newParentDirectoryRecord.Id;
                                }
                                break;

                            // renamed a not-directory
                            default:
                                FileDropFile fileRecord = db.FileDropFile
                                                            .Include(f => f.Directory)
                                                            .Where(f => f.Directory.FileDropId == connection.FileDropId)
                                                            .SingleOrDefault(f => EF.Functions.ILike(f.FileName, Path.GetFileName(evtData.Path)));
                                if (fileRecord == null)
                                {
                                    evtData.StatusCode = 4;  // SSH_FX_FAILURE 4
                                    return;
                                }

                                fileRecord.FileName = Path.GetFileName(evtData.NewPath);

                                string newCanonicalDirectoryPath = FileDropDirectory.ConvertPathToCanonicalPath(Path.GetDirectoryName(evtData.NewPath));
                                if (!newCanonicalDirectoryPath.Equals(fileRecord.Directory.CanonicalFileDropPath, StringComparison.InvariantCultureIgnoreCase))
                                { // This move involves a change in containing directory
                                    FileDropDirectory newDirectoryRecord = db.FileDropDirectory
                                                                             .Where(d => d.FileDropId == connection.FileDropId)
                                                                             .SingleOrDefault(d => EF.Functions.ILike(d.CanonicalFileDropPath, newCanonicalDirectoryPath));
                                    if (newDirectoryRecord == null)
                                    {
                                        evtData.StatusCode = 4;  // SSH_FX_FAILURE 4
                                        return;
                                    }

                                    fileRecord.DirectoryId = newDirectoryRecord.Id;
                                }
                                break;
                        }

                        db.SaveChanges();

                        Log.Information($"Renamed {evtData.Path} to {evtData.NewPath} in FileDrop <{connection.FileDropName}> (Id {connection.FileDropId}).  Account {connection.Account?.UserName} (Id {connection.Account?.Id})");
                        new AuditLogger().Log(AuditEventType.SftpRename.ToEvent(new SftpRenameLogModel
                        {
                            From = evtData.Path,
                            To = evtData.NewPath,
                            IsDirectory = (attributes & FileAttributes.Directory) == FileAttributes.Directory,
                            FileDrop = new FileDropLogModel { Id = connection.FileDropId.Value, Name = connection.FileDropName },
                            Account = connection.Account,
                            User = connection.MapUser,
                        }));
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directoryRecord">Recursion only occurs on child directory records contained in the ChildDirectories navigation property</param>
        /// <param name="toPath">Must meet requirements to be converted to a canonical path as described in <see cref="FileDropDirectory.CanonicalFileDropPath"/></param>
        private static void RepathDirectoryRecord(FileDropDirectory directoryRecord, string toPath, bool recurseOnChildren)
        {
            directoryRecord.FileDropPath = toPath;

            if (recurseOnChildren)
            {
                foreach (FileDropDirectory child in directoryRecord.ChildDirectories)
                {
                    RepathDirectoryRecord(child, Path.Combine(toPath, Path.GetFileName(child.CanonicalFileDropPath)), recurseOnChildren);
                }
            }
        }

        //[Description("Fires when a client wants to write to an open file.")]
        internal static void OnFileWrite(object sender, SftpserverFileWriteEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("FileWrite", evtData));
        }

        //[Description("Fires when a client attempts to set file or directory attributes.")]
        internal static void OnSetAttributes(object sender, SftpserverSetAttributesEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("SetAttributes", evtData));
        }

        //[Description("Fires when a client attempts to authenticate a connection.")]
        internal static void OnSSHUserAuthRequest(object sender, SftpserverSSHUserAuthRequestEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("SSHUserAuthRequest", evtData));

            //_sftpServer.Config($"UserAuthBanner[{evtData.ConnectionId}]=Whatever");
            if (evtData.AuthMethod.Equals("password", StringComparison.InvariantCultureIgnoreCase))
            {
                (AuthorizationResult result, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.None);

                // Documentation for this event is at http://cdn.nsoftware.com/help/IHF/cs/SFTPServer_e_DirCreate.htm
                switch (result)
                {
                    case AuthorizationResult.ConnectionNotFound:
                        return;
                    case AuthorizationResult.NotAuthorized:
                        connection.LastActivityUtc = DateTime.UtcNow;
                        return;
                }

                connection.LastActivityUtc = DateTime.UtcNow;

                using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
                {
                    SftpAccount userAccount = db.SftpAccount
                                                .Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                .Where(a => !a.IsSuspended)
                                                .Where(a => !a.FileDrop.IsSuspended)
                                                .Include(a => a.ApplicationUser)
                                                .Include(a => a.FileDropUserPermissionGroup)
                                                    .ThenInclude(g => g.FileDrop)
                                                        .ThenInclude(d => d.Client)
                                                .SingleOrDefault(a => a.UserName == evtData.User);

                    if (userAccount == null)
                    {
                        evtData.Accept = false;
                        Log.Information($"SftpConnection request denied.  An account with permission to a FileDrop was not found, requested account name is <{evtData.User}>");
                        // TODO is an audit log called for here?
                        return;
                    }

                    if (!userAccount.IsCurrent(GlobalResources.ApplicationConfiguration.GetValue("SftpPasswordExpirationDays", 60)))
                    {
                        evtData.Accept = false;
                        Log.Information($"SftpConnection request denied.  The requested account with name <{evtData.User}> has an expired password");
                        return;
                    }

                    if (userAccount.ApplicationUserId.HasValue && (!userAccount.ApplicationUser.IsCurrent(GlobalResources.ApplicationConfiguration.GetValue("PasswordExpirationDays", 60)) || userAccount.ApplicationUser.IsSuspended))
                    {
                        evtData.Accept = false;
                        Log.Information($"SftpConnection request denied.  The MAP user <{userAccount.ApplicationUser.UserName}> associated with this SFTP account has an expired password or is suspended");
                        return;
                    }

                    var passwordResult = userAccount.CheckPassword(evtData.AuthParam);

                    if (passwordResult == PasswordVerificationResult.Success ||
                        passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        string absoluteFileDropRootPath = Path.Combine(GlobalResources.ApplicationConfiguration.GetValue<string>("Storage:FileDropRoot"), userAccount.FileDropUserPermissionGroup.FileDrop.RootPath);

                        // set this connection's root path
                        IpWorksSftpServer._sftpServer.Config($@"UserRootDirectory[{evtData.ConnectionId}]={absoluteFileDropRootPath}");

                        connection.LastActivityUtc = DateTime.UtcNow;
                        connection.MapUser = userAccount.ApplicationUser != null ? new ApplicationUser { Id = userAccount.ApplicationUser.Id, UserName = userAccount.ApplicationUser.UserName} : null;
                        connection.Account = new SftpAccount(userAccount.FileDropId) { Id = userAccount.Id, UserName = userAccount.UserName };
                        connection.FileDropId = userAccount.FileDropId;
                        connection.FileDropName = userAccount.FileDropUserPermissionGroup.FileDrop.Name;
                        connection.ClientId = userAccount.FileDropId;
                        connection.ClientName = userAccount.FileDropUserPermissionGroup.FileDrop.Client.Name;
                        connection.ReadAccess = userAccount.FileDropUserPermissionGroup.ReadAccess;
                        connection.WriteAccess = userAccount.FileDropUserPermissionGroup.WriteAccess;
                        connection.DeleteAccess = userAccount.FileDropUserPermissionGroup.DeleteAccess;
                        connection.FileDropRootPathAbsolute = absoluteFileDropRootPath;

                        evtData.Accept = true;

                        Log.Information($"Sftp acount <{userAccount.UserName}> authenticated, FileDrop is <{userAccount.FileDrop.Name}>, access is: " +
                                        (userAccount.FileDropUserPermissionGroupId.HasValue
                                        ? "read: " + userAccount.FileDropUserPermissionGroup.ReadAccess.ToString() +
                                            ", write: " + userAccount.FileDropUserPermissionGroup.WriteAccess.ToString() +
                                            ", delete: " + userAccount.FileDropUserPermissionGroup.DeleteAccess.ToString()
                                        : "no permission group assigned"));
                    }
                }
            }
        }

        //[Description("Shows the progress of the secure connection.")]
        internal static void OnSSHStatus(object sender, SftpserverSSHStatusEventArgs evtData)
        {
            //Log.Information(GenerateEventArgsLogMessage("SSHStatus", evtData));
        }

        internal static void OnMaintenanceTimerElapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
            {
                int passwordExpirationDays = GlobalResources.ApplicationConfiguration.GetValue("PasswordExpirationDays", 60);
                int sftpPasswordExpirationDays = GlobalResources.ApplicationConfiguration.GetValue("SftpPasswordExpirationDays", 60);

                List<Guid> currentConnectedAccountIds = IpWorksSftpServer._connections
                                                                         .Where(c => c.Value != null)  // .Value can be null in some debugging situations
                                                                         .Where(c => c.Value.Account != null)
                                                                         .Select(c => c.Value.Account.Id)
                                                                         .Distinct()
                                                                         .ToList();

                var query = db.SftpAccount
                              .Include(a => a.ApplicationUser)
                              .Include(a => a.FileDropUserPermissionGroup)
                                  .ThenInclude(p => p.FileDrop)
                              .Where(a => currentConnectedAccountIds.Contains(a.Id));

                foreach (SftpAccount connectedAccount in query)
                {
                    // There can be multiple connections for a user
                    foreach (SftpConnectionProperties connection in IpWorksSftpServer._connections.Values.Where(c => c.Account?.Id == connectedAccount.Id))
                    {
                        if (connection == null)  // can happen during debug if a connection is closed while sitting at a breakpoint in this method
                        {
                            continue;
                        }

                        if (!connectedAccount.IsCurrent(sftpPasswordExpirationDays) ||
                            connectedAccount.FileDropUserPermissionGroup.FileDrop.IsSuspended ||
                            (connectedAccount.ApplicationUserId.HasValue && !connectedAccount.ApplicationUser.IsCurrent(passwordExpirationDays)))
                        {
                            IpWorksSftpServer._connections.Remove(connection.Id);
                        }
                        else
                        {
                            connection.ReadAccess = connectedAccount.FileDropUserPermissionGroup.ReadAccess;
                            connection.WriteAccess = connectedAccount.FileDropUserPermissionGroup.WriteAccess;
                            connection.DeleteAccess = connectedAccount.FileDropUserPermissionGroup.DeleteAccess;
                        }
                    }
                }
            }
        }

        #region Async event completion handlers
        internal static void ShutdownCompleted(object sender, SftpserverAsyncCompletedEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage($"ShutdownCompleted, {IpWorksSftpServer._connections.Count} connections will be deleted", evtData));
            IpWorksSftpServer._connections.Clear();
        }

        internal static void SetFileListCompleted(object sender, SftpserverAsyncCompletedEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("SetFileListCompleted", evtData));
        }
        internal static void ExchangeKeysCompleted(object sender, SftpserverAsyncCompletedEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("ExchangeKeysCompleted", evtData));
        }
        internal static void DisconnectCompleted(object sender, SftpserverAsyncCompletedEventArgs evtData)
        {
            Log.Information(GenerateEventArgsLogMessage("DisconnectCompleted", evtData));
        }
        #endregion

        #endregion

        protected static string GenerateEventArgsLogMessage(string eventName, EventArgs args)
        {
            return $"{eventName} event with EventArgs: {JsonSerializer.Serialize(args, args.GetType(), prettyJsonOptions)}";

            // Some event data has escaped characters (e.g. " => \0022, \r => <CR>, \n => <LF>, etc.). The following unescapes those.
            //return $"{DateTime.UtcNow.ToString("u")} {eventName} event with EventArgs: {Regex.Unescape(JsonSerializer.Serialize(args, args.GetType(), prettyJsonOptions))}";
        }

        protected enum RequiredAccess
        {
            None,
            Read,
            Write,
            Delete
        }

        protected enum AuthorizationResult
        {
            ConnectionNotFound,
            NotAuthorized,
            Authorized,
        }

        protected static (AuthorizationResult result, SftpConnectionProperties connection) GetAuthorizedConnectionProperties(string connectionId, RequiredAccess requiredAccess)
        {
            if (!IpWorksSftpServer._connections.Keys.Contains(connectionId))
            {
                return (AuthorizationResult.ConnectionNotFound, null);
            }

            var connectionRecord = IpWorksSftpServer._connections[connectionId];

            bool accountHasAccess = false;

            switch (requiredAccess)
            {
                case RequiredAccess.None:
                    accountHasAccess = true;
                    break;
                case RequiredAccess.Read:
                    accountHasAccess = connectionRecord.ReadAccess;
                    break;
                case RequiredAccess.Write:
                    accountHasAccess = connectionRecord.WriteAccess;
                    break;
                case RequiredAccess.Delete:
                    accountHasAccess = connectionRecord.DeleteAccess;
                    break;
            }

            return (
                accountHasAccess 
                    ? AuthorizationResult.Authorized 
                    : AuthorizationResult.NotAuthorized, 
                connectionRecord);

        }

    }
}

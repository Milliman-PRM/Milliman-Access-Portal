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
using MapDbContextLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using nsoftware.IPWorksSSH;
using Prm.EmailQueue;
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
            // Documentation for this event is at http://cdn.nsoftware.com/help/IHF/cs/SFTPServer_e_FileRemove.htm

            Log.Verbose(GenerateEventArgsLogMessage("FileRemove", evtData));

            (AuthorizationResult result, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.Delete);

            if (result == AuthorizationResult.ConnectionNotFound)
            {
                Log.Warning("OnFileRemove: Connection not found, eventdata: {@evtData}", evtData);
                evtData.StatusCode = 7;  // SSH_FX_CONNECTION_LOST 7
                return;
            }

            connection.LastActivityUtc = DateTime.UtcNow;

            if (result == AuthorizationResult.NotAuthorized)
            {
                Log.Warning("OnFileRemove: Permission denied, eventdata: {@evtData}", evtData);
                evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                return;
            }

            using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
            {
                string requestedFileName = Path.GetFileName(evtData.Path);
                string requestedDirectoryCanonicalPath = FileDropDirectory.ConvertPathToCanonicalPath(Path.GetDirectoryName(evtData.Path));
                string requestedAbsolutePath = Path.Combine(connection.FileDropRootPathAbsolute, evtData.Path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                FileDropFile fileRecord = db.FileDropFile
                                            .Include(f => f.Directory)
                                            .Where(f => EF.Functions.ILike(requestedDirectoryCanonicalPath, f.Directory.CanonicalFileDropPath))
                                            .Where(f => f.Directory.FileDropId == connection.FileDropId)
                                            .SingleOrDefault(f => EF.Functions.ILike(requestedFileName, f.FileName));

                if (evtData.BeforeExec)
                {
                    bool failValidation = false;
                    if (fileRecord == null)
                    {
                        Log.Warning($"OnFileRemove: Requested file {evtData.Path} at absolute path {requestedAbsolutePath} is not found in the database, FileDrop ID {connection.FileDropId}, named {connection.FileDropName}");
                        failValidation = true;
                    }
                    else if (!File.Exists(requestedAbsolutePath))
                    {
                        Log.Warning($"OnFileRemove: Requested file {evtData.Path} at absolute path {requestedAbsolutePath} is not found in the file system), FileDrop ID {connection.FileDropId}, named {connection.FileDropName}");
                        failValidation = true;
                    }
                    if (failValidation) 
                    {
                        evtData.StatusCode = 10;  // SSH_FX_NO_SUCH_PATH 10
                        return;
                    }
                }
                else
                {
                    if (evtData.StatusCode == 0)
                    {
                        db.FileDropFile.Remove(fileRecord);
                        db.SaveChanges();

                        new AuditLogger().Log(AuditEventType.SftpFileRemoved.ToEvent((FileDropFileLogModel)fileRecord,
                                                                                     (FileDropDirectoryLogModel)fileRecord.Directory,
                                                                                          new FileDropLogModel { Id = connection.FileDropId.Value, Name = connection.FileDropName },
                                                                                          connection.Account,
                                                                                          connection.MapUser), connection.MapUser?.UserName);
                        Log.Information($"OnFileRemove: Requested file {evtData.Path} at absolute path {requestedAbsolutePath} removed, FileDrop ID {connection.FileDropId}, named {connection.FileDropName}");
                    }
                    else
                    {
                        Log.Error($"OnFileRemove: Requested file {evtData.Path} at absolute path {requestedAbsolutePath} failed to be removed, event status {evtData.StatusCode}, FileDrop ID {connection.FileDropId}, named {connection.FileDropName}");
                    }
                }


            }
        }

        //[Description("Fires when a client wants to read from an open file.")]
        internal static void OnFileRead(object sender, SftpserverFileReadEventArgs evtData)
        {
            // Documentation for this event is at http://cdn.nsoftware.com/help/IHF/cs/SFTPServer_e_FileRead.htm
            // This event occurs between OnFileOpen and OnFileClose, only to document transfer of a block of file data
            Log.Verbose(GenerateEventArgsLogMessage("FileRead", evtData));

            (AuthorizationResult result, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.Read);

            if (result != AuthorizationResult.Authorized || !connection.ReadAccess)
            {
                Log.Information($"OnFileRead event invoked but account <{connection.Account?.Id}, {connection.Account?.UserName}> does not have Read access");
                evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                return;
            }
        }

        //[Description("Fires when a client wants to open or create a file.")]
        internal static void OnFileOpen(object sender, SftpserverFileOpenEventArgs evtData)
        {
            Log.Verbose(GenerateEventArgsLogMessage("FileOpen", evtData));

            // Documentation for this event is at http://cdn.nsoftware.com/help/IHF/cs/SFTPServer_e_FileOpen.htm

            RequiredAccess requiredAccess = evtData.Flags switch
            {
                int flags when (flags & 0x00000001) == 0x00000001 => RequiredAccess.Read,  // SSH_FXF_READ (0x00000001)
                int flags when (flags & 0x00000002) == 0x00000002 => RequiredAccess.Write, // SSH_FXF_WRITE (0x00000002)
                _ => RequiredAccess.NoRequirement
            };
            if (requiredAccess == RequiredAccess.NoRequirement)
            {
                Log.Warning($"OnFileOpen event invoked for unsupported event data flags value {evtData.Flags:X8}");
                evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                return;
            }

            (AuthorizationResult authResult, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, requiredAccess);
            if (authResult == AuthorizationResult.ConnectionNotFound)
            {
                Log.Warning($"OnFileOpen: the requested connection id {evtData.ConnectionId} was not found for account {evtData.User} and required access {requiredAccess}");
                evtData.StatusCode = 7;  // SSH_FX_CONNECTION_LOST 7
                return;
            }

            connection.LastActivityUtc = DateTime.UtcNow;

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

            if (evtData.BeforeExec)
            {
                if (authResult == AuthorizationResult.NotAuthorized)
                {
                    Log.Information($"OnFileOpen event invoked for file {requiredAccess}, but access is denied for account {evtData.User}");
                    evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                    return;
                }

                if (containingDirectory == null)
                {
                    Log.Warning($"OnFileOpen event invoked for file {requiredAccess}, but containing directory {dirPathToFind} database record was not foundc");
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

                            FileDropFile newFileDropFile = new FileDropFile
                            {
                                FileName = fileName,
                                DirectoryId = containingDirectory.Id,
                                CreatedByAccountId = connection.Account.Id,
                            };
                            db.FileDropFile.Add(newFileDropFile);
                            db.SaveChanges();
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
            else
            {
                switch (evtData.Flags)
                {
                    case int flags when (flags & 0x00000008) == 0x00000008  // SSH_FXF_CREAT (0x00000008)
                                     && (flags & 0x00000002) == 0x00000002  // SSH_FXF_WRITE (0x00000002)
                                     && evtData.FileType == 1:   // SSH_FILEXFER_TYPE_REGULAR(1)
                        // create/write a file

                        using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
                        {
                            Guid fileDropFileId = db.FileDropFile
                                                    .Where(f => f.DirectoryId == containingDirectory.Id)
                                                    .Where(f => f.FileName == fileName)
                                                    .Select(f => f.Id)
                                                    .Single();
                            connection.OpenFileWrites.Add(evtData.Handle, fileDropFileId);

                            #region Process user notifications for FileWrite event
                            List<SftpAccount> accountsToNotify = db.SftpAccount
                                                                   .Include(a => a.ApplicationUser)
                                                                   .Where(a => a.FileDropUserPermissionGroup.FileDropId == connection.FileDropId)
                                                                   .Where(a => !a.IsSuspended)
                                                                   .Where(a => !a.ApplicationUser.IsSuspended)
                                                                   .ToList();
                            if (accountsToNotify.Any())
                            {
                                List<ApplicationUser> usersToNotify = accountsToNotify.Where(a => a.NotificationSubscriptions.Any(n => n.NotificationType == FileDropNotificationType.FileWrite
                                                                                                                                    && n.IsEnabled))
                                                                                      .Select(a => a.ApplicationUser)
                                                                                      .ToList();
                                string subject = "MAP file drop notification";
                                string message = $"File \"{evtData.Path.TrimStart('/')}\" has been uploaded to file drop \"{connection.FileDropName}\". {Environment.NewLine}{Environment.NewLine}" +
                                    $"You are subscribed to MAP notifications for this file drop. " +
                                    $"To manage your notifications, log into MAP and go to \"My Settings\" for file drop \"{connection.FileDropName}\". ";

                                MailSender mailSender = new MailSender();
                                foreach (ApplicationUser user in usersToNotify)
                                {
                                    mailSender.QueueMessage(user.Email, subject, message, "map.support@milliman.com", "Milliman Access Portal notifications");
                                }
                            }
                            #endregion

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

                        break;

                    default:
                        evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                        return;
                }
            }
        }

        //[Description("Fires when a client attempts to close an open file or directory handle.")]
        internal static async void OnFileClose(object sender, SftpserverFileCloseEventArgs evtData)
        {
            Log.Verbose(GenerateEventArgsLogMessage("FileClose", evtData));

            (AuthorizationResult result, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.Write);

            if (result != AuthorizationResult.ConnectionNotFound)
            {
                connection.LastActivityUtc = DateTime.UtcNow;

                if (connection.OpenFileWrites.TryGetValue(evtData.Handle, out Guid fileDropFileId) && evtData.StatusCode == 0)
                {
                    string absoluteFilePath = Path.Combine(connection.FileDropRootPathAbsolute, evtData.Path.TrimStart('/', '\\'));

                    using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
                    {
                        try
                        {
                            var fileDropFileRecord = await db.FileDropFile.FindAsync(fileDropFileId);
                            FileInfo fileInfo = new FileInfo(absoluteFilePath);

                            fileDropFileRecord.UploadDateTimeUtc = DateTime.UtcNow;
                            fileDropFileRecord.Size = fileInfo.Length;
                            await db.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            // TODO Do something reasonable
                        }
                        finally
                        {
                            connection.OpenFileWrites.Remove(evtData.Handle);
                        }
                    }
                }
            }
        }

        //[Description("Fired when a connection is closed.")]
        internal static void OnDisconnected(object sender, SftpserverDisconnectedEventArgs evtData)
        {
            var connectionProperties = IpWorksSftpServer._connections[evtData.ConnectionId];

            if (connectionProperties.Account != null)
            {
                Log.Information($"Connection <{evtData.ConnectionId}> closed for account <{connectionProperties.Account.UserName}>");
            }
            else
            {
                Log.Information($"Connection <{evtData.ConnectionId}> closed");
            }

            IpWorksSftpServer._connections.Remove(evtData.ConnectionId);
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
                                                                                      connection.MapUser), connection.MapUser?.UserName);
                    Log.Information($"OnDirRemove: Requested directory {evtData.Path} at absolute path {requestedAbsolutePath} removed. Deleted inventory is {{@Inventory}}", deleteInventory);
                }
            }
        }

        //[Description("Fires when a client attempts to open a directory for listing.")]
        internal static void OnDirList(object sender, SftpserverDirListEventArgs evtData)
        {
            Log.Verbose(GenerateEventArgsLogMessage("DirList", evtData));
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
                                                                                          connection.MapUser), connection.MapUser?.UserName);
                        Log.Information($"DirCreate event invoked, directory {requestedCanonicalPath} created relative to UserRootDirectory { IpWorksSftpServer._sftpServer.Config($"UserRootDirectory[{connection.Id}]")}");
                    }
                }
            }
        }

        //[Description("Fired when a request for connection comes from a remote host.")]
        internal static void OnConnectionRequest(object sender, SftpserverConnectionRequestEventArgs evtData)
        {
            Log.Verbose(GenerateEventArgsLogMessage("ConnectionRequest", evtData));
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

            Log.Information($"New connection <{evtData.ConnectionId}> accepted from remote host {IpWorksSftpServer._sftpServer.Connections[evtData.ConnectionId]?.RemoteHost}");
        }

        //[Description("Fires when a client needs to get file information.")]
        internal static void OnGetAttributes(object sender, SftpserverGetAttributesEventArgs evtData)
        {
            Log.Verbose(GenerateEventArgsLogMessage("GetAttributes", evtData));

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
            Log.Verbose(GenerateEventArgsLogMessage("Log", evtData));
            //Log.Debug($"Event OnLog: Args: {JsonSerializer.Serialize(evtData, prettyJsonOptions)}");
        }

        //[Description("Fires when a client attempts to canonicalize a path.")]
        internal static void OnResolvePath(object sender, SftpserverResolvePathEventArgs evtData)
        {
            Log.Verbose(GenerateEventArgsLogMessage("ResolvePath", evtData));
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
                        }), connection.MapUser?.UserName);
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
        // Documentation for this event is at http://cdn.nsoftware.com/help/IHF/cs/SFTPServer_e_FileWrite.htm
            Log.Verbose(GenerateEventArgsLogMessage("FileWrite", evtData));

            if (evtData.BeforeExec)
            {
                (AuthorizationResult result, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.Write);

                if (result != AuthorizationResult.Authorized || !connection.WriteAccess)
                {
                    Log.Information($"OnFileWrite event invoked but account <{connection.Account?.Id}, {connection.Account?.UserName}> does not have Write access");
                    evtData.StatusCode = 3;  // SSH_FX_PERMISSION_DENIED 3
                    return;
                }
            }
        }

        //[Description("Fires when a client attempts to set file or directory attributes.")]
        internal static void OnSetAttributes(object sender, SftpserverSetAttributesEventArgs evtData)
        {
            Log.Verbose(GenerateEventArgsLogMessage("SetAttributes", evtData));
        }

        //[Description("Fires when a client attempts to authenticate a connection.")]
        internal static void OnSSHUserAuthRequest(object sender, SftpserverSSHUserAuthRequestEventArgs evtData)
        {
            // Documentation for this event is at http://cdn.nsoftware.com/help/IHF/cs/SFTPServer_e_DirCreate.htm
            Log.Verbose(GenerateEventArgsLogMessage("SSHUserAuthRequest", evtData));

            string clientAddress = IpWorksSftpServer._sftpServer.Connections[evtData.ConnectionId]?.RemoteHost;

            if (evtData.AuthMethod.Equals("password", StringComparison.InvariantCultureIgnoreCase))
            {
                (AuthorizationResult result, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.NoRequirement);

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
                                                    .ThenInclude(u => u.AuthenticationScheme)
                                                .Include(a => a.FileDrop)
                                                .Include(a => a.FileDropUserPermissionGroup)
                                                    .ThenInclude(g => g.FileDrop)
                                                        .ThenInclude(d => d.Client)
                                                .SingleOrDefault(a => EF.Functions.ILike(evtData.User, a.UserName));

                    if (userAccount == null)
                    {
                        evtData.Accept = false;
                        Log.Information($"SftpConnection request from remote host <{clientAddress}> denied.  An account with permission to a FileDrop was not found, requested account name is <{evtData.User}>");
                        if (!string.IsNullOrWhiteSpace(evtData.User))
                        {
                            userAccount = new SftpAccount(Guid.Empty) { UserName = evtData.User };
                            new AuditLogger().Log(AuditEventType.SftpAuthenticationFailed.ToEvent(userAccount, AuditEventType.SftpAuthenticationFailReason.UserNotFound, null, clientAddress));
                        }
                        return;
                    }

                    if (userAccount.IsSuspended)
                    {
                        evtData.Accept = false;
                        Log.Information($"SftpConnection request from remote host <{clientAddress}> denied.  The requested account with name <{evtData.User}> is suspended");
                        new AuditLogger().Log(AuditEventType.SftpAuthenticationFailed.ToEvent(userAccount, AuditEventType.SftpAuthenticationFailReason.AccountSuspended, (FileDropLogModel)userAccount.FileDrop, clientAddress));
                        return;
                    }

                    int sftpPasswordExpirationDays = GlobalResources.ApplicationConfiguration.GetValue("SftpPasswordExpirationDays", 60);
                    if (DateTime.UtcNow - userAccount.PasswordResetDateTimeUtc > TimeSpan.FromDays(sftpPasswordExpirationDays))
                    {
                        evtData.Accept = false;
                        Log.Information($"SftpConnection request from remote host <{clientAddress}> denied.  The requested account with name <{evtData.User}> has an expired password");
                        new AuditLogger().Log(AuditEventType.SftpAuthenticationFailed.ToEvent(userAccount, AuditEventType.SftpAuthenticationFailReason.PasswordExpired, (FileDropLogModel)userAccount.FileDrop, clientAddress));
                        return;
                    }

                    int mapPasswordExpirationDays = GlobalResources.ApplicationConfiguration.GetValue("PasswordExpirationDays", 60);
                    bool userIsSso = false;
                    if (userAccount.ApplicationUser != null)
                    {
                        userIsSso = IsSsoUser(userAccount.ApplicationUser);

                        if (userAccount.ApplicationUser.IsSuspended || 
                            (!userIsSso && DateTime.UtcNow - userAccount.ApplicationUser.LastPasswordChangeDateTimeUtc > TimeSpan.FromDays(mapPasswordExpirationDays)))
                        {
                            evtData.Accept = false;
                            Log.Information($"SftpConnection request from remote host <{clientAddress}> denied.  The related MAP user with name <{evtData.User}> has an expired password or is suspended");
                            new AuditLogger().Log(AuditEventType.SftpAuthenticationFailed.ToEvent(userAccount, AuditEventType.SftpAuthenticationFailReason.MapUserBlocked, (FileDropLogModel)userAccount.FileDrop, clientAddress));
                            return;
                        }
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
                        connection.MapUserIsSso = userIsSso;

                        evtData.Accept = true;

                        Log.Information($"Acount <{userAccount.UserName}> authenticated on connection {evtData.ConnectionId} from remote host <{clientAddress}>, FileDrop <{userAccount.FileDrop.Name}>, access: " +
                                        (userAccount.FileDropUserPermissionGroupId.HasValue
                                        ? "read:" + userAccount.FileDropUserPermissionGroup.ReadAccess.ToString() +
                                          ", write:" + userAccount.FileDropUserPermissionGroup.WriteAccess.ToString() +
                                          ", delete:" + userAccount.FileDropUserPermissionGroup.DeleteAccess.ToString()
                                        : "no permission group assigned"));
                    }
                    else
                    {
                        new AuditLogger().Log(AuditEventType.SftpAuthenticationFailed.ToEvent(userAccount, AuditEventType.SftpAuthenticationFailReason.AuthenticationFailed, (FileDropLogModel)userAccount.FileDrop, clientAddress));
                        Log.Information($"Acount <{userAccount.UserName}> authentication failed from remote host <{clientAddress}> for FileDrop <{userAccount.FileDrop.Name}>");
                    }
                }
            }
        }

        //[Description("Shows the progress of the secure connection.")]
        internal static void OnSSHStatus(object sender, SftpserverSSHStatusEventArgs evtData)
        {
            Log.Verbose(GenerateEventArgsLogMessage("SSHStatus", evtData));
        }

        internal static void OnMaintenanceTimerElapsed(object source, System.Timers.ElapsedEventArgs e)
        {
            using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
            {
                int mapPasswordExpirationDays = GlobalResources.ApplicationConfiguration.GetValue("PasswordExpirationDays", 60);
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
                    // An account can have multiple open connections
                    foreach (SftpConnectionProperties connection in IpWorksSftpServer._connections.Values.Where(c => c.Account?.Id == connectedAccount.Id))
                    {
                        if (connection == null)  // can happen during debug if a connection is closed while sitting at a breakpoint in this method
                        {
                            continue;
                        }

                        if (!connectedAccount.IsCurrent(sftpPasswordExpirationDays))
                        {
                            IpWorksSftpServer._sftpServer.DisconnectAsync(connection.Id);
                            IpWorksSftpServer._connections.Remove(connection.Id);
                            Log.Information($"Connection {connection.Id} for account {connection.Account.UserName} disconnecting because the SFTP account is suspended or has expired password");
                        }
                        else if (connectedAccount.FileDropUserPermissionGroup == null)
                        {
                            IpWorksSftpServer._sftpServer.DisconnectAsync(connection.Id);
                            IpWorksSftpServer._connections.Remove(connection.Id);
                            Log.Information($"Connection {connection.Id} for account {connection.Account.UserName} disconnecting because the SFTP account currently is not authorized through a permission group");
                        }
                        else if (connectedAccount.FileDropUserPermissionGroup.FileDrop.IsSuspended)
                        {
                            IpWorksSftpServer._sftpServer.DisconnectAsync(connection.Id);
                            IpWorksSftpServer._connections.Remove(connection.Id);
                            Log.Information($"Connection {connection.Id} for account {connection.Account.UserName} disconnecting because the file drop is suspended");
                        }

                        else if (connectedAccount.ApplicationUserId.HasValue && 
                                  (connectedAccount.ApplicationUser.IsSuspended ||
                                    !(connection.MapUserIsSso || DateTime.UtcNow - connectedAccount.ApplicationUser.LastPasswordChangeDateTimeUtc < TimeSpan.FromDays(mapPasswordExpirationDays))))
                        {
                            IpWorksSftpServer._sftpServer.DisconnectAsync(connection.Id);
                            IpWorksSftpServer._connections.Remove(connection.Id);
                            Log.Information($"Connection {connection.Id} for account {connection.Account.UserName} disconnecting because the related MAP user is suspended or is locally authenticated and has expired password");
                        }
                        else
                        {
                            if (
                                connection.ReadAccess != connectedAccount.FileDropUserPermissionGroup.ReadAccess ||
                                connection.WriteAccess != connectedAccount.FileDropUserPermissionGroup.WriteAccess ||
                                connection.DeleteAccess != connectedAccount.FileDropUserPermissionGroup.DeleteAccess
                            )
                            {
                                Log.Information($"Maintenance handler modifying user {connectedAccount.UserName} cached permissions for connection <{connection.Id}> " +
                                    $"Read was {connection.ReadAccess}, now {connectedAccount.FileDropUserPermissionGroup.ReadAccess}; " +
                                    $"Write was {connection.WriteAccess}, now {connectedAccount.FileDropUserPermissionGroup.WriteAccess}; " +
                                    $"Delete was {connection.DeleteAccess}, now {connectedAccount.FileDropUserPermissionGroup.DeleteAccess}");

                                connection.ReadAccess = connectedAccount.FileDropUserPermissionGroup.ReadAccess;
                                connection.WriteAccess = connectedAccount.FileDropUserPermissionGroup.WriteAccess;
                                connection.DeleteAccess = connectedAccount.FileDropUserPermissionGroup.DeleteAccess;
                            }

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
            NoRequirement,
            AnyOneOrMore,
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
                Log.Warning($"Connection <{connectionId}> not found among all tracked connections ({string.Join(',', IpWorksSftpServer._connections.Keys.ToArray())})");
                return (AuthorizationResult.ConnectionNotFound, null);
            }

            var connectionRecord = IpWorksSftpServer._connections[connectionId];

            bool accountHasAccess = false;

            switch (requiredAccess)
            {
                case RequiredAccess.NoRequirement:
                    accountHasAccess = true;
                    break;

                case RequiredAccess.AnyOneOrMore:
                    accountHasAccess = connectionRecord.ReadAccess
                                    || connectionRecord.WriteAccess
                                    || connectionRecord.DeleteAccess;
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

            if (accountHasAccess)
            {
                return (AuthorizationResult.Authorized, connectionRecord);
            }
            else
            {
                Log.Debug($"Connection {connectionId} exists but does not have required access {requiredAccess}");
                return (AuthorizationResult.NotAuthorized, connectionRecord);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user">Must have AuthenticationScheme navigation property present</param>
        /// <returns></returns>
        protected static bool IsSsoUser(ApplicationUser user)
        {
            using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
            {
                if (user?.AuthenticationScheme != null)
                {
                    // user has an explicit individually assigned scheme
                    return user.AuthenticationScheme.Type != AuthenticationType.Default;
                }
                else
                {
                    string userFullDomain = user.UserName.Contains('@')
                                    ? user.UserName.Substring(user.UserName.IndexOf('@') + 1)
                                    : user.UserName;

                    if (db.AuthenticationScheme.Any(s => s.DomainList.Contains(userFullDomain) && s.Type != AuthenticationType.Default))
                    {
                        // domain of username is contained in the domain list of a scheme 
                        return true;
                    }
                    else if (userFullDomain.Contains('.'))
                    {
                        // Secondary domain (the portion of userName between '@' and the last '.') matches a scheme name
                        string userSecondaryDomain = userFullDomain.Substring(0, userFullDomain.LastIndexOf('.'));
                        return db.AuthenticationScheme.Any(s => EF.Functions.ILike(s.Name, userSecondaryDomain));
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

    }
}

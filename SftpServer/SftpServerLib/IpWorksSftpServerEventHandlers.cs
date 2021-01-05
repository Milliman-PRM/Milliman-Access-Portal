/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using AuditLogLib.Models;
using FileDropLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MapDbContextLib.Models;
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
        internal static Dictionary<string, SftpConnectionProperties> _connections;

        static IpWorksSftpServerEventHandlers()
        {
            _connections = new Dictionary<string, SftpConnectionProperties>();
        }

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

            evtData.StatusCode = (int)FileDropOperations.RemoveFile(evtData.Path, 
                                                                    connection.FileDropName, 
                                                                    connection.FileDropRootPathAbsolute, 
                                                                    connection.FileDropId, 
                                                                    connection.Account, 
                                                                    connection.MapUser, 
                                                                    evtData.BeforeExec, 
                                                                    evtData.StatusCode);
        }

        //[Description("Fires when a client wants to read from an open file.")]
        internal static void OnFileRead(object sender, SftpserverFileReadEventArgs evtData)
        {
            // Documentation for this event is at http://cdn.nsoftware.com/help/IHF/cs/SFTPServer_e_FileRead.htm
            // This event occurs between OnFileOpen and OnFileClose, only to document transfer of a block of file data
            Log.Verbose(GenerateEventArgsLogMessage("FileRead", evtData));

            (AuthorizationResult result, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.Read);

            if (connection != null)
            {
                connection.LastActivityUtc = DateTime.UtcNow;
            }

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
            using (ApplicationDbContext db = FileDropOperations.NewMapDbContext)
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

                        using (ApplicationDbContext db = FileDropOperations.NewMapDbContext)
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
                                CreatedByAccountUserName = connection.Account.UserName,
                            };
                            db.FileDropFile.Add(newFileDropFile);
                            db.SaveChanges();
                        }
                        break;

                    case int flags when (flags & 0x00000001) == 0x00000001  // SSH_FXF_READ (0x00000001)
                                     && evtData.FileType == 1:   // SSH_FILEXFER_TYPE_REGULAR(1)
                        // read a file

                        using (ApplicationDbContext db = FileDropOperations.NewMapDbContext)
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

                        using (ApplicationDbContext db = FileDropOperations.NewMapDbContext)
                        {
                            Guid fileDropFileId = db.FileDropFile
                                                    .Where(f => f.DirectoryId == containingDirectory.Id)
                                                    .Where(f => f.FileName == fileName)
                                                    .Select(f => f.Id)
                                                    .Single();
                            connection.OpenFileWrites.Add(evtData.Handle, fileDropFileId);

                            FileDropOperations.HandleUserNotifications(connection.FileDropId.GetValueOrDefault(), connection.FileDropName, evtData.Path, FileDropNotificationType.FileWrite);

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
                            Log.Information($"File {evtData.Path} authorized for writing, connection {evtData.ConnectionId}, user {evtData.User}");
                        }
                        break;

                    case int flags when (flags & 0x00000001) == 0x00000001  // SSH_FXF_READ (0x00000001)
                                     && evtData.FileType == 1:   // SSH_FILEXFER_TYPE_REGULAR(1)
                        // read a file

                        using (ApplicationDbContext db = FileDropOperations.NewMapDbContext)
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
                            Log.Information($"File {evtData.Path} authorized for reading, connection {evtData.ConnectionId}, user {evtData.User}");
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
            (AuthorizationResult result, SftpConnectionProperties connection) = GetAuthorizedConnectionProperties(evtData.ConnectionId, RequiredAccess.Write);

            if (result != AuthorizationResult.ConnectionNotFound)
            {
                connection.LastActivityUtc = DateTime.UtcNow;

                if (connection.OpenFileWrites.TryGetValue(evtData.Handle, out Guid fileDropFileId) && evtData.StatusCode == 0)
                {
                    connection.OpenFileWrites.Remove(evtData.Handle);
                    string absoluteFilePath = Path.Combine(connection.FileDropRootPathAbsolute, evtData.Path.TrimStart('/', '\\'));

                    using (ApplicationDbContext db = FileDropOperations.NewMapDbContext)
                    {
                        try
                        {
                            var fileDropFileRecord = db.FileDropFile.Find(fileDropFileId);
                            FileInfo fileInfo = new FileInfo(absoluteFilePath);

                            fileDropFileRecord.UploadDateTimeUtc = DateTime.UtcNow;
                            fileDropFileRecord.Size = fileInfo.Length;
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to update FileDropFile record with time stamp and size in OnFileClose");
                        }
                    }
                }
            }
            Log.Verbose($"File closed, connection {evtData.ConnectionId}, user {evtData.User}, path {evtData.Path}");
        }

        //[Description("Fired when a connection is closed.")]
        internal static void OnDisconnected(object sender, SftpserverDisconnectedEventArgs evtData)
        {
            if (_connections.TryGetValue(evtData.ConnectionId, out var connectionProperties))
            {
                Log.Information($"Connection <{evtData.ConnectionId}> closed for account <{connectionProperties.Account?.UserName ?? "<unknown>"}>");
            }
            else
            {
                Log.Information($"Connection <{evtData.ConnectionId}> closed");
            }

            _connections.Remove(evtData.ConnectionId);
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

            evtData.StatusCode = (int)FileDropOperations.RemoveDirectory(evtData.Path,
                                                                          connection.FileDropName,
                                                                          connection.FileDropRootPathAbsolute,
                                                                          connection.FileDropId,
                                                                          connection.Account,
                                                                          connection.MapUser,
                                                                          evtData.BeforeExec,
                                                                          evtData.StatusCode);
        }

        //[Description("Fires when a client attempts to open a directory for listing.")]
        internal static void OnDirList(object sender, SftpserverDirListEventArgs evtData)
        {
            Log.Verbose(GenerateEventArgsLogMessage("DirList", evtData));
        }

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

            evtData.StatusCode = (int)FileDropOperations.CreateDirectory(evtData.Path,
                                                                         connection.FileDropRootPathAbsolute,
                                                                         connection.FileDropName,
                                                                         string.Empty,
                                                                         connection.FileDropId,
                                                                         connection.ClientId,
                                                                         connection.ClientName,
                                                                         connection.Account,
                                                                         connection.MapUser,
                                                                         evtData.BeforeExec,
                                                                         evtData.StatusCode);
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

            _connections[evtData.ConnectionId] = newConnection;
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

            FileAttributes attributes = FileAttributes.Offline;
            try
            {
                if (evtData.BeforeExec == true)
                {
                    attributes = File.GetAttributes(evtData.Path);
                }
                else
                {
                    attributes = File.GetAttributes(Path.Combine(connection.FileDropRootPathAbsolute, evtData.NewPath.TrimStart('/', '\\')));
                }
            }
            catch (Exception ex)
            {
                evtData.StatusCode = 2;  // SSH_FX_NO_SUCH_FILE 2
                return;
            }

            switch (attributes)
            {
                // rename directory
                case FileAttributes a when (a & FileAttributes.Directory) == FileAttributes.Directory:
                    evtData.StatusCode = (int)FileDropOperations.RenameDirectory(evtData.Path,
                                                                                 evtData.NewPath,
                                                                                 connection.FileDropRootPathAbsolute,
                                                                                 connection.FileDropName,
                                                                                 connection.FileDropId,
                                                                                 connection.ClientId,
                                                                                 connection.ClientName,
                                                                                 connection.Account,
                                                                                 connection.MapUser,
                                                                                 evtData.BeforeExec,
                                                                                 evtData.StatusCode);
                    break;

                // rename file
                default:
                    evtData.StatusCode = (int)FileDropOperations.RenameFile(evtData.Path,
                                                                            evtData.NewPath,
                                                                            connection.FileDropRootPathAbsolute,
                                                                            connection.FileDropName,
                                                                            connection.FileDropId,
                                                                            connection.ClientId,
                                                                            connection.ClientName,
                                                                            connection.Account,
                                                                            connection.MapUser,
                                                                            evtData.BeforeExec,
                                                                            evtData.StatusCode);
                    break;
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

                if (connection != null)
                {
                    connection.LastActivityUtc = DateTime.UtcNow;
                }

                if (result != AuthorizationResult.Authorized || !connection.WriteAccess || !connection.OpenFileWrites.ContainsKey(evtData.Handle))
                {
                    Log.Information($"OnFileWrite event invoked but account <{connection.Account?.Id}, {connection.Account?.UserName}> does not have Write access, or write handle <{evtData.Handle}> is not known");
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

                using (ApplicationDbContext db = FileDropOperations.NewMapDbContext)
                {
                    SftpAccount userAccount = db.SftpAccount
                                                .Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                .Where(a => a.FileDropUserPermissionGroup.ReadAccess || a.FileDropUserPermissionGroup.WriteAccess || a.FileDropUserPermissionGroup.DeleteAccess)
                                                .Where(a => !a.IsSuspended)
                                                .Where(a => !a.FileDrop.IsSuspended)
                                                .Include(a => a.ApplicationUser)
                                                    .ThenInclude(u => u.AuthenticationScheme)
                                                .Include(a => a.FileDrop)
                                                .Include(a => a.FileDropUserPermissionGroup)
                                                    .ThenInclude(g => g.FileDrop)
                                                        .ThenInclude(d => d.Client)
                                                .SingleOrDefault(a => EF.Functions.ILike(evtData.User, a.UserName));

                    int clientReviewRenewalPeriodDays = GlobalResources.ApplicationConfiguration.GetValue<int>("ClientReviewRenewalPeriodDays");
                    DateTime clientReviewDeadline = userAccount.FileDropUserPermissionGroup.FileDrop.Client.LastAccessReview.LastReviewDateTimeUtc + TimeSpan.FromDays(clientReviewRenewalPeriodDays);

                    if (userAccount == null)
                    {
                        evtData.Accept = false;
                        Log.Information($"Sftp authentication request on connection {evtData.ConnectionId} from remote host <{clientAddress}> denied.  An account with permission to a FileDrop was not found, requested account name is <{evtData.User}>");
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
                        Log.Information($"Sftp authentication request on connection {evtData.ConnectionId} from remote host <{clientAddress}> denied.  The requested account with name <{evtData.User}> is suspended");
                        new AuditLogger().Log(AuditEventType.SftpAuthenticationFailed.ToEvent(userAccount, AuditEventType.SftpAuthenticationFailReason.AccountSuspended, (FileDropLogModel)userAccount.FileDrop, clientAddress));
                        return;
                    }

                    if (DateTime.UtcNow > clientReviewDeadline)
                    {
                        evtData.Accept = false;
                        Log.Information($"Sftp authentication request on connection {evtData.ConnectionId} from remote host <{clientAddress}> denied.  The access review deadline for the client related to the requested file drop is exceeded");
                        new AuditLogger().Log(AuditEventType.SftpAuthenticationFailed.ToEvent(userAccount, AuditEventType.SftpAuthenticationFailReason.ClientAccessReviewDeadlineMissed, (FileDropLogModel)userAccount.FileDrop, clientAddress));
                        return;
                    }

                    int sftpPasswordExpirationDays = GlobalResources.ApplicationConfiguration.GetValue("SftpPasswordExpirationDays", 60);
                    if (DateTime.UtcNow - userAccount.PasswordResetDateTimeUtc > TimeSpan.FromDays(sftpPasswordExpirationDays))
                    {
                        evtData.Accept = false;
                        Log.Information($"Sftp authentication request on connection {evtData.ConnectionId} remote host <{clientAddress}> denied.  The requested account with name <{evtData.User}> has an expired password");
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
                            Log.Information($"Sftp authentication request on connection {evtData.ConnectionId} from remote host <{clientAddress}> denied.  The related MAP user with name <{evtData.User}> has an expired password or is suspended");
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
                        connection.ClientAccessReviewDeadline = userAccount.FileDropUserPermissionGroup.FileDrop.Client.LastAccessReview.LastReviewDateTimeUtc + TimeSpan.FromDays(clientReviewRenewalPeriodDays);
                        connection.ReadAccess = userAccount.FileDropUserPermissionGroup.ReadAccess;
                        connection.WriteAccess = userAccount.FileDropUserPermissionGroup.WriteAccess;
                        connection.DeleteAccess = userAccount.FileDropUserPermissionGroup.DeleteAccess;
                        connection.FileDropRootPathAbsolute = absoluteFileDropRootPath;
                        connection.MapUserIsSso = userIsSso;

                        userAccount.LastLoginUtc = DateTime.UtcNow;
                        db.SaveChanges();

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
                        Log.Information($"Acount <{userAccount.UserName}> authentication failed on connection {evtData.ConnectionId} from remote host <{clientAddress}> for FileDrop <{userAccount.FileDrop.Name}>");
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
            using (ApplicationDbContext db = FileDropOperations.NewMapDbContext)
            {
                int mapPasswordExpirationDays = GlobalResources.ApplicationConfiguration.GetValue("PasswordExpirationDays", 60);
                int sftpPasswordExpirationDays = GlobalResources.ApplicationConfiguration.GetValue("SftpPasswordExpirationDays", 60);

                List<Guid> currentConnectedAccountIds = _connections.Where(c => c.Value != null)  // .Value can be null in some debugging situations
                                                                    .Where(c => c.Value.Account != null)
                                                                    .Select(c => c.Value.Account.Id)
                                                                    .Distinct()
                                                                    .ToList();

                var query = db.SftpAccount
                              .Include(a => a.ApplicationUser)
                              .Include(a => a.FileDropUserPermissionGroup)
                                  .ThenInclude(p => p.FileDrop)
                                    .ThenInclude(d => d.Client)
                              .Where(a => currentConnectedAccountIds.Contains(a.Id));

                foreach (SftpAccount connectedAccount in query)
                {
                    // An account can have multiple open connections
                    foreach (SftpConnectionProperties connection in _connections.Values.Where(c => c.Account?.Id == connectedAccount.Id))
                    {
                        if (connection == null)  // can happen during debug if a connection is closed while sitting at a breakpoint in this method
                        {
                            continue;
                        }

                        if (!connectedAccount.IsCurrent(sftpPasswordExpirationDays))
                        {
                            IpWorksSftpServer._sftpServer.DisconnectAsync(connection.Id);
                            _connections.Remove(connection.Id);
                            Log.Information($"Connection {connection.Id} for account {connection.Account.UserName} disconnecting because the SFTP account is suspended or has expired password");
                        }
                        else if (connectedAccount.FileDropUserPermissionGroup == null)
                        {
                            IpWorksSftpServer._sftpServer.DisconnectAsync(connection.Id);
                            _connections.Remove(connection.Id);
                            Log.Information($"Connection {connection.Id} for account {connection.Account.UserName} disconnecting because the SFTP account currently is not authorized through a permission group");
                        }
                        else if (connectedAccount.FileDropUserPermissionGroup.FileDrop.IsSuspended)
                        {
                            IpWorksSftpServer._sftpServer.DisconnectAsync(connection.Id);
                            _connections.Remove(connection.Id);
                            Log.Information($"Connection {connection.Id} for account {connection.Account.UserName} disconnecting because the file drop is suspended");
                        }
                        else if (DateTime.UtcNow > connection.ClientAccessReviewDeadline)
                        {
                            IpWorksSftpServer._sftpServer.DisconnectAsync(connection.Id);
                            _connections.Remove(connection.Id);
                            Log.Information($"Connection {connection.Id} for account {connection.Account.UserName} disconnecting because the client access review deadline has passed");
                        }

                        else if (connectedAccount.ApplicationUserId.HasValue && 
                                  (connectedAccount.ApplicationUser.IsSuspended ||
                                    !(connection.MapUserIsSso || DateTime.UtcNow - connectedAccount.ApplicationUser.LastPasswordChangeDateTimeUtc < TimeSpan.FromDays(mapPasswordExpirationDays))))
                        {
                            IpWorksSftpServer._sftpServer.DisconnectAsync(connection.Id);
                            _connections.Remove(connection.Id);
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
            Log.Information(GenerateEventArgsLogMessage($"ShutdownCompleted, {_connections.Count} connections will be deleted", evtData));
            _connections.Clear();
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

        public enum AuthorizationResult
        {
            ConnectionNotFound,
            NotAuthorized,
            Authorized,
        }

        protected static (AuthorizationResult result, SftpConnectionProperties connection) GetAuthorizedConnectionProperties(string connectionId, RequiredAccess requiredAccess)
        {
            if (!_connections.Keys.Contains(connectionId))
            {
                Log.Warning($"Connection <{connectionId}> not found among all tracked connections ({string.Join(',', _connections.Keys.ToArray())})");
                return (AuthorizationResult.ConnectionNotFound, null);
            }

            var connectionRecord = _connections[connectionId];

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
            using (ApplicationDbContext db = FileDropOperations.NewMapDbContext)
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

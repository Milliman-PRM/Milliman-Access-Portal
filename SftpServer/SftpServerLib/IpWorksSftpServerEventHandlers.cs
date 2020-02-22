/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using MapDbContextLib.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using nsoftware.IPWorksSSH;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            Debug.WriteLine(GenerateEventArgsLogMessage("Error", evtData));
        }

        //[Description("Fires when a client wants to delete a file.")]
        internal static void OnFileRemove(object sender, SftpserverFileRemoveEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("FileRemove", evtData));
        }

        //[Description("Fires when a client wants to read from an open file.")]
        internal static void OnFileRead(object sender, SftpserverFileReadEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("FileRead", evtData));
        }

        //[Description("Fires when a client wants to open or create a file.")]
        internal static void OnFileOpen(object sender, SftpserverFileOpenEventArgs evtData)
        {
            //evtData.StatusCode = 3;  // to block the action, set a status

            Debug.WriteLine(GenerateEventArgsLogMessage("FileOpen", evtData));
        }

        //[Description("Fires when a client attempts to close an open file or directory handle.")]
        internal static void OnFileClose(object sender, SftpserverFileCloseEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("FileClose", evtData));
        }

        //[Description("Fired when a connection is closed.")]
        internal static void OnDisconnected(object sender, SftpserverDisconnectedEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("Disconnected", evtData));

            using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
            {
                SftpConnection thisConnection = db.SftpConnection
                                                    .Include(c => c.SftpAccount)
                                                    .SingleOrDefault(c => c.Id == evtData.ConnectionId);

                if (thisConnection != null)
                {
                    db.SftpConnection.Remove(thisConnection);
                    db.SaveChanges();

                    Log.Information($"Connection {evtData.ConnectionId} closed for account name {thisConnection.SftpAccount.UserName}");
                }
            }
        }

        //[Description("Fires when a client wants to delete a directory.")]
        internal static void OnDirRemove(object sender, SftpserverDirRemoveEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("DirRemove", evtData));
        }

        //[Description("Fires when a client attempts to open a directory for listing.")]
        internal static void OnDirList(object sender, SftpserverDirListEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("DirList", evtData));
        }

        //[Description("Fires when a client wants to create a new directory.")]
        internal static void OnDirCreate(object sender, SftpserverDirCreateEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("DirCreate", evtData));
            Debug.WriteLine($"Path above is relative to UserRootDirectory {IpWorksSftpServer._sftpServer.Config($"UserRootDirectory[{evtData.ConnectionId}]")}");
        }

        //[Description("Fired when a request for connection comes from a remote host.")]
        internal static void OnConnectionRequest(object sender, SftpserverConnectionRequestEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("ConnectionRequest", evtData));
        }

        //[Description("Fired immediately after a connection completes (or fails).")]
        internal static void OnConnected(object sender, SftpserverConnectedEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("Connected", evtData));
        }

        //[Description("Fires when a client needs to get file information.")]
        internal static void OnGetAttributes(object sender, SftpserverGetAttributesEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("GetAttributes", evtData));

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
            Debug.WriteLine(GenerateEventArgsLogMessage("Log", evtData));
            Debug.WriteLine($"Event OnLog: Args: {JsonSerializer.Serialize(evtData, prettyJsonOptions)}");
        }

        //[Description("Fires when a client attempts to canonicalize a path.")]
        internal static void OnResolvePath(object sender, SftpserverResolvePathEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("ResolvePath", evtData));
        }

        //[Description("Fires when a client wants to rename a file.")]
        internal static void OnFileRename(object sender, SftpserverFileRenameEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("FileRename", evtData));
        }

        //[Description("Fires when a client wants to write to an open file.")]
        internal static void OnFileWrite(object sender, SftpserverFileWriteEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("FileWrite", evtData));
        }

        internal static void ShutdownCompleted(object sender, SftpserverAsyncCompletedEventArgs evtData)
        {
            using (var db = GlobalResources.NewMapDbContext)
            {
                var connections = db.SftpConnection.ToList();
                db.SftpConnection.RemoveRange(connections);
                Debug.WriteLine(GenerateEventArgsLogMessage($"ShutdownCompleted, {connections.Count} connection records dropped", evtData));
            }
        }

        internal static void SetFileListCompleted(object sender, SftpserverAsyncCompletedEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("SetFileListCompleted", evtData));
        }

        //[Description("Fires when a client attempts to set file or directory attributes.")]
        internal static void OnSetAttributes(object sender, SftpserverSetAttributesEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("SetAttributes", evtData));
        }

        internal static void ExchangeKeysCompleted(object sender, SftpserverAsyncCompletedEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("ExchangeKeysCompleted", evtData));
        }

        internal static void DisconnectCompleted(object sender, SftpserverAsyncCompletedEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("DisconnectCompleted", evtData));
        }

        //[Description("Fires when a client attempts to authenticate a connection.")]
        internal static void OnSSHUserAuthRequest(object sender, SftpserverSSHUserAuthRequestEventArgs evtData)
        {
            Debug.WriteLine(GenerateEventArgsLogMessage("SSHUserAuthRequest", evtData));

            //_sftpServer.Config($"UserAuthBanner[{evtData.ConnectionId}]=Whatever");
            if (evtData.AuthMethod.Equals("password", StringComparison.InvariantCultureIgnoreCase))
            {
                using (ApplicationDbContext db = GlobalResources.NewMapDbContext)
                {
                    SftpAccount userAccount = db.SftpAccount
                                                .Where(a => a.FileDropUserPermissionGroupId.HasValue)
                                                .Include(a => a.ApplicationUser)
                                                .Include(a => a.FileDropUserPermissionGroup)
                                                    .ThenInclude(g => g.FileDrop)
                                                .SingleOrDefault(a => a.UserName == evtData.User);

                    if (userAccount == null)
                    {
                        evtData.Accept = false;
                        Log.Information($"SftpConnection request denied.  An account with permission to a FileDrop was not found, requested account name is <{evtData.User}>");
                        // TODO is an audit log called for here?
                        return;
                    }

                    var passwordVerification = userAccount.CheckPassword(evtData.AuthParam);
                    if (passwordVerification == PasswordVerificationResult.Success ||
                        passwordVerification == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        IpWorksSftpServer._sftpServer.Config($@"UserRootDirectory[{evtData.ConnectionId}]={Path.Combine(GlobalResources.ApplicationConfiguration.GetValue<string>("FileDropRoot"), userAccount.FileDropUserPermissionGroup.FileDrop.RootPath)}");

                        SftpConnection connection = new SftpConnection
                        {
                            Id = evtData.ConnectionId,
                            CreatedDateTimeUtc = DateTime.UtcNow,
                            LastActivityUtc = DateTime.UtcNow,
                            SftpAccountId = userAccount.Id,
                        };
                        db.SftpConnection.Add(connection);
                        db.SaveChanges();

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
            //Debug.WriteLine(GenerateEventArgsLogMessage("SSHStatus", evtData));
        }

        #endregion

        protected static string GenerateEventArgsLogMessage(string eventName, EventArgs args)
        {
            return $"{DateTime.UtcNow.ToString("u")} {eventName} event with EventArgs: {JsonSerializer.Serialize(args, args.GetType(), prettyJsonOptions)}";

            // Some event data has escaped characters (e.g. " => \0022, \r => <CR>, \n => <LF>, etc.). The following unescapes those.
            //return $"{DateTime.UtcNow.ToString("u")} {eventName} event with EventArgs: {Regex.Unescape(JsonSerializer.Serialize(args, args.GetType(), prettyJsonOptions))}";
        }

    }
}

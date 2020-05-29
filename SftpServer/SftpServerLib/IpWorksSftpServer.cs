/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Concrete implementation of the MAP SFTP server (excluding event handlers)
 * DEVELOPER NOTES: This class is partial.  Implementation is contained in multiple source code files
 */

using AuditLogLib;
using Microsoft.Extensions.Configuration;
using nsoftware.IPWorksSSH;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SftpServerLib
{
    internal partial class IpWorksSftpServer : SftpLibApi
    {
        internal static System.Timers.Timer _maintenanceTimer;
        internal static Sftpserver _sftpServer;
        internal static Dictionary<string, SftpConnectionProperties> _connections;
        internal static AuditLogger _auditLogger;

        static IpWorksSftpServer()
        {
            _connections = new Dictionary<string, SftpConnectionProperties>();
        }
        internal IpWorksSftpServer() 
        {
            // At launch all connection records should be dropped because the sftp library reuses connection IDs. 
            AuditLogger.Config = new AuditLoggerConfiguration
            {
                AuditLogConnectionString = GlobalResources.GetConnectionString("AuditLogConnectionString"),
                //ErrorLogRootFolder = "",  // TODO need to deal with this?
            };

            _auditLogger = new AuditLogger();

            _maintenanceTimer = new System.Timers.Timer
            {
                //Interval = GlobalResources.ApplicationConfiguration.GetValue<double>("SftpMaintenanceIntervalMs", 15_000),
                Interval = GlobalResources.GetConfigValue<double>("SftpMaintenanceIntervalMs", 500),
                AutoReset = true,
                Enabled = true,
            };
            _maintenanceTimer.Elapsed += IpWorksSftpServerEventHandlers.OnMaintenanceTimerElapsed;
        }
        
        public override void Start(byte[] keyBytes)
        {
            Certificate certificate = new Certificate(keyBytes);
            EstablishServerInstance(certificate);

            _sftpServer.Listening = true;
        }

        public override void Stop()
        {
            if (_sftpServer != null)
            {
                try
                {
                    _sftpServer.Shutdown();
                    _sftpServer = null;
                    Log.Information("SFTP server shut down");
                }
                catch (Exception e)
                {
                    var ex = e;
                    Log.Error("Exception during SFTP server shutdown");
                }
            }
        }

        public override ServerState ReportState()
        {
            if (_sftpServer == null)
            {
                return null;
            }

            return new ServerState
            {
                Fingerprint = _sftpServer.SSHCert.Fingerprint,
                About = _sftpServer.About,
                LocalPort = _sftpServer.LocalPort,
            };
        }

        private protected void EstablishServerInstance(Certificate cert)
        {
            _sftpServer = new Sftpserver
            {
                RootDirectory = GlobalResources.GetConfigValue<string>("FileDropRoot"),
                SSHCert = cert,
                RuntimeLicense = "31484E4641443153554232303231303231335241454E545032444D30474B30300000000000000000345444484443435700004D594E4A59423758584E47320000,"
            };
            Log.Verbose("SFTP Server instance constructed");

            #region assign event handlers
            //[Description("Information about errors during data delivery.")]
            _sftpServer.OnError += IpWorksSftpServerEventHandlers.OnError;
            //[Description("Fires when a client wants to delete a file.")]
            _sftpServer.OnFileRemove += IpWorksSftpServerEventHandlers.OnFileRemove;
            //[Description("Fires when a client wants to read from an open file.")]
            _sftpServer.OnFileRead += IpWorksSftpServerEventHandlers.OnFileRead;
            //[Description("Fires when a client wants to open or create a file.")]
            _sftpServer.OnFileOpen += IpWorksSftpServerEventHandlers.OnFileOpen;
            //[Description("Fires when a client attempts to close an open file or directory handle.")]
            _sftpServer.OnFileClose += IpWorksSftpServerEventHandlers.OnFileClose;
            //[Description("Fired when a connection is closed.")]
            _sftpServer.OnDisconnected += IpWorksSftpServerEventHandlers.OnDisconnected;
            //[Description("Fires when a client wants to delete a directory.")]
            _sftpServer.OnDirRemove += IpWorksSftpServerEventHandlers.OnDirRemove;
            //[Description("Fires when a client attempts to open a directory for listing.")]
            _sftpServer.OnDirList += IpWorksSftpServerEventHandlers.OnDirList;
            //[Description("Fires when a client wants to create a new directory.")]
            _sftpServer.OnDirCreate += IpWorksSftpServerEventHandlers.OnDirCreate;
            //[Description("Fired when a request for connection comes from a remote host.")]
            _sftpServer.OnConnectionRequest += IpWorksSftpServerEventHandlers.OnConnectionRequest;
            //[Description("Fired immediately after a connection completes (or fails).")]
            _sftpServer.OnConnected += IpWorksSftpServerEventHandlers.OnConnected;
            //[Description("Fires when a client needs to get file information.")]
            _sftpServer.OnGetAttributes += IpWorksSftpServerEventHandlers.OnGetAttributes;
            //[Description("Fires once for each log message.")]
            //_sftpServer.OnLog += IpWorksSftpServerEventHandlers.OnLog;
            //[Description("Fires when a client attempts to canonicalize a path.")]
            _sftpServer.OnResolvePath += IpWorksSftpServerEventHandlers.OnResolvePath;
            //[Description("Fires when a client wants to rename a file.")]
            _sftpServer.OnFileRename += IpWorksSftpServerEventHandlers.OnFileRename;
            //[Description("Fires when a client wants to write to an open file.")]
            _sftpServer.OnFileWrite += IpWorksSftpServerEventHandlers.OnFileWrite;
            //[Description("Fires when a client attempts to set file or directory attributes.")]
            _sftpServer.OnSetAttributes += IpWorksSftpServerEventHandlers.OnSetAttributes;
            //[Description("Fires when a client attempts to authenticate a connection.")]
            _sftpServer.OnSSHUserAuthRequest += IpWorksSftpServerEventHandlers.OnSSHUserAuthRequest;
            //[Description("Shows the progress of the secure connection.")]
            _sftpServer.OnSSHStatus += IpWorksSftpServerEventHandlers.OnSSHStatus;

            _sftpServer.ShutdownCompleted += IpWorksSftpServerEventHandlers.ShutdownCompleted;
            _sftpServer.SetFileListCompleted += IpWorksSftpServerEventHandlers.SetFileListCompleted;
            _sftpServer.ExchangeKeysCompleted += IpWorksSftpServerEventHandlers.ExchangeKeysCompleted;
            _sftpServer.DisconnectCompleted += IpWorksSftpServerEventHandlers.DisconnectCompleted;

            Log.Verbose("SFTP Server event handlers assigned");
            #endregion
        }

    }
}

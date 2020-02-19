/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Concrete implementation of the MAP SFTP server
 * DEVELOPER NOTES: This class is partial.  Implementation is contained in multiple source code files
 */

using Microsoft.Extensions.Configuration;
using nsoftware.IPWorksSSH;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SftpServerLib
{
    internal partial class IpWorksSftpServer : SftpLibApi
    {
        internal IpWorksSftpServer(IConfigurationRoot configurationRoot) 
            : base(configurationRoot)
        {
            MapDbConnectionString = _applicationConfiguration.GetConnectionString("DefaultConnection");

            AuditLogLib.AuditLogger.Config = new AuditLogLib.AuditLoggerConfiguration
            {
                AuditLogConnectionString = _applicationConfiguration.GetConnectionString("AuditLogConnectionString"),
                ErrorLogRootFolder = "",  // TODO need to deal with this?
            };
        }

        protected Sftpserver _sftpServer = default;

        public override void Start(byte[] keyBytes)
        {
            Certificate certificate = new Certificate(keyBytes);
            EstablishServerInstance(certificate);

            _sftpServer.Listening = true;
            Debug.WriteLine("Server listening");
        }

        public async override void Stop()
        {
            try
            {
                await _sftpServer.ShutdownAsync();
                _sftpServer = null;
                Debug.WriteLine("Server instance destroyed");
            }
            catch (Exception e)
            {
                var ex = e;
                Debug.WriteLine("Exception while stopping server");
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
            };
        }
    }
}

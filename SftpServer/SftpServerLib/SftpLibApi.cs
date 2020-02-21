/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines the API signatures consumed by users of SFTP functionality
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SftpServerLib
{
    /// <summary>
    /// An API definition class to hide the inheriting implementation.  Also contains some universal code
    /// </summary>
    public abstract class SftpLibApi
    {
        public static SftpLibApi NewInstance()
        {
            return new IpWorksSftpServer();
        }

        public abstract void Start(byte[] certificateBytes);

        public abstract void Stop();

        public abstract ServerState ReportState();
    }

    public class ServerState
    {
        public string Fingerprint { get; set; }
    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines the API signatures consumed by users of SFTP functionality
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Microsoft.AspNetCore.Identity;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SftpServerLib
{
    public abstract class SftpLibApi
    {
        public static SftpLibApi NewInstance()
        {
            return new IpWorksSftpServer();
        }

        internal SftpLibApi()
        {
            //throw new NotImplementedException("Abstract base class SftpLibApi cannot be instantiated");
        }

        public abstract void Start(byte[] certificateBytes);

        public abstract void Stop();

        public abstract ServerState ReportState();

        public virtual bool Authenticate(string ConnectionId, string UserName, string Password)
        {
            if (UserName.Equals("anonymous", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            else
            {
                // authenticate, return true for success, false for fail
            }

            return false;
        }

    }

    public class ServerState
    {
        public string Fingerprint { get; set; }
    }
}

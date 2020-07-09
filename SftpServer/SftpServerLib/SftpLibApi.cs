/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines the API signatures consumed by users of SFTP functionality
 * DEVELOPER NOTES: <What future developers need to know.>
 */

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
        public string About { get; set; }
        public int LocalPort { get; set; }
    }
}

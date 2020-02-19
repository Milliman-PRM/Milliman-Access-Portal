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
    public abstract class SftpLibApi
    {
        protected IConfigurationRoot _applicationConfiguration = null;

        public static SftpLibApi NewInstance(IConfigurationRoot configurationRoot)
        {
            return new IpWorksSftpServer(configurationRoot);
        }

        internal SftpLibApi(IConfigurationRoot configurationRoot)
        {
            _applicationConfiguration = configurationRoot;

            // Initialize Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_applicationConfiguration)
                .CreateLogger();

            /*
            Assembly processAssembly = Assembly.GetEntryAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(processAssembly.Location);
            Log.Information($"Process launched:{Environment.NewLine}" +
                            $"\tProduct Name <{fileVersionInfo.ProductName}>{Environment.NewLine}" +
                            $"\tAssembly version <{fileVersionInfo.ProductVersion}>{Environment.NewLine}" +
                            $"\tAssembly location <{processAssembly.Location}>{Environment.NewLine}" +
                            $"\tASPNETCORE_ENVIRONMENT = <{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}>{Environment.NewLine}");
            */

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

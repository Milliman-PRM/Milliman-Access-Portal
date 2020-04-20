/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using SftpServerLib;
//using Microsoft.Extensions.Configuration.
using System;
using System.IO;
using System.Text;

namespace SftpServer
{
    class Program
    {
        static SftpLibApi _SftpApi = null;

        static void Main(string[] args)
        {
            GlobalResources.LoadConfiguration();
            GlobalResources.InitializeSerilog(GlobalResources.ApplicationConfiguration);

            _SftpApi = SftpLibApi.NewInstance();

            string privateKeyString = GlobalResources.GetConfigValue<string>("SftpServerPrivateKey");
            byte[] privateKeyBytes = Encoding.UTF8.GetBytes(privateKeyString);

            _SftpApi.Start(privateKeyBytes);

            var state = _SftpApi.ReportState();
            Console.WriteLine($"SFTP server listening on port {state.LocalPort}");

            Console.WriteLine("Press any key to terminate this application");
            Console.ReadKey(true);
            Console.WriteLine("Terminating...");

            _SftpApi.Stop();
            Console.WriteLine("Server stopped");
        }
    }
}

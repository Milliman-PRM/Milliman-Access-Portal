/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Serilog;
using SftpServerLib;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SftpServer
{
    class Program
    {
        static readonly CancellationTokenSource Cts = new CancellationTokenSource();

        static SftpLibApi _SftpApi = null;

        static async Task Main(string[] args)
        {
            GlobalResources.LoadConfiguration();

            _SftpApi = SftpLibApi.NewInstance();

            string privateKeyString = GlobalResources.GetConfigValue<string>("SftpServerPrivateKey").Replace(@"\n", "\n");
            byte[] privateKeyBytes = Encoding.UTF8.GetBytes(privateKeyString);

            _SftpApi.Start(privateKeyBytes);

            var state = _SftpApi.ReportState();
            Console.WriteLine($"SFTP server listening on port {state.LocalPort}");
            Console.WriteLine($"SFTP server fingerprint is {state.Fingerprint}");

            Task WaitForKeyTask = Task.Factory.StartNew(() => CancelWhenTerminationIndicated());

            // Terminate when the CancellationToken is canceled
            try
            {
                await Task.Delay(Timeout.Infinite, Cts.Token);
            }
            catch (TaskCanceledException)
            {
                Log.Information("Terminating...");
                _SftpApi.Stop();
                Log.Information("Server stopped");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception while expecting termination event");
            }

            Log.CloseAndFlush();
        }

        private static void CancelWhenTerminationIndicated()
        {
            while (true)
            {
                try
                {
                    // If needed, use multiple termination techniques e.g. keystroke for Windows + SIGTERM handler for linux container
                    Console.WriteLine("Press any key to terminate this application");
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    Cts.Cancel();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error waiting for external termination instruction");
                    Thread.Sleep(1000);
                }
            }
        }
    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Serilog;
using SftpServerLib;
using System;
using System.Diagnostics;
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
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => Cts.Cancel();

            Console.CancelKeyPress += (sender, eventArgs) => 
            {
                // Ctrl-C or Ctrl-Break pressed
                eventArgs.Cancel = true;  // Allow the graceful shutdown to complete
                Cts.Cancel();
            };

            GlobalResources.LoadConfiguration();

            _SftpApi = SftpLibApi.NewInstance();

            string privateKeyString = GlobalResources.GetConfigValue<string>("SftpServerPrivateKey").Replace(@"\n", "\n");
            byte[] privateKeyBytes = Encoding.UTF8.GetBytes(privateKeyString);

            _SftpApi.Start(privateKeyBytes);

            var state = _SftpApi.ReportState();
            Console.WriteLine($"SFTP server listening on port {state.LocalPort}");
            Console.WriteLine($"SFTP server fingerprint is {state.Fingerprint}");

            Task WaitForKeyTask = CancelWhenTerminationIndicated();

            // Terminate when the CancellationToken is canceled
            try
            {
                await Task.Delay(Timeout.Infinite, Cts.Token);
            }
            catch (TaskCanceledException)
            {
                StopSftpServer();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception while expecting termination event");
            }

            Log.Information("The process is exiting");
            Log.CloseAndFlush();
            Environment.Exit(0);  // seems to be needed when ctl-C
        }

        private static void StopSftpServer()
        {
            Log.Information("Terminating normally...");
            _SftpApi.Stop();
            _SftpApi = null;
        }

        private async static Task CancelWhenTerminationIndicated()
        {
            Console.WriteLine("Press any key to terminate this application");
            Task readKeyTask = Task.Run(() => Console.ReadKey(true), Cts.Token);

            while (true)
            {
                try
                {
                    await readKeyTask;
                    Cts.Cancel();
                    break;
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

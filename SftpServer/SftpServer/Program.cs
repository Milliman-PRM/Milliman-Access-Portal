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
                Log.Information("Ctl-C, Ctl-Break, or equivalent linux signal received");
                eventArgs.Cancel = true;  // Allow the graceful shutdown code to run
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

            Task KeyPressTask = Task.Run(() => CancelTokenOnConsoleKeyPress());

            try
            {
                // Block this thread indefinitely, until token is canceled
                await Task.Delay(Timeout.Infinite, Cts.Token);
            }
            catch (TaskCanceledException)
            {
                _SftpApi.Stop();
                _SftpApi = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception while expecting termination event");
            }

            Log.Information("The process is exiting");
            Log.CloseAndFlush();
            Environment.Exit(0);  // seems to be needed when ctl-C
        }

        private static void CancelTokenOnConsoleKeyPress()
        {
            Console.WriteLine("Press any key to terminate this application");

            Console.ReadKey(true);

            Log.Information("Normal user initiated termination");
            Cts.Cancel();
        }
    }
}

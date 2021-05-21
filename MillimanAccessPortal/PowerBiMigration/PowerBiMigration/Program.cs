using Serilog;
using System;
using System.Reflection;
using System.IO;
using System.Windows.Forms;

namespace PowerBiMigration
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string logFilePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            logFilePath = Path.Combine(logFilePath, "log.txt");

            // Initialize Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .WriteTo.Console()
                .WriteTo.File(logFilePath)
                .MinimumLevel.Debug()
                .CreateLogger();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}

using System;
using System.Threading;
using AuditLogLib;

namespace AuditLogLibTest
{
    class Program
    {
        static void Main(string[] args)
        {
            AuditLogger L = new AuditLogger();

            for (int i=0; i < 100; i++)
            {
                L.Log(Microsoft.Extensions.Logging.LogLevel.Critical, 1, "xyz");
                Thread.Sleep(2000);
            }
        }
    }
}
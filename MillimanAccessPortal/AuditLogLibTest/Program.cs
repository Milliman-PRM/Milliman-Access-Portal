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
                L.Log(Microsoft.Extensions.Logging.LogLevel.Critical, AuditEventId.LoginFailure, new
                {
                    String1 = "xyz",
                    Array1 = new int[] { 1,2,3,4 },
                    Object1 = new
                    {
                        Subfield1 = 11,
                        Subfield2 = "Value2",
                        Subfield3 = 4.56,
                    }
                });
                Thread.Sleep(2000);
            }
        }
    }
}
using System;
using System.Threading;
using AuditLogLib;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using AuditLogLib.Event;

namespace AuditLogLibTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // This code demonstrates usage of the AuditLogger as a directly instantiated object, 
            // rather than as accessed through the ILoggerProvider interface implementation.  It works both ways. 

            AuditLogger.Config = new AuditLoggerConfiguration { AuditLogConnectionString = "127.0.0.1;Database=MapAuditLog;User Id=postgres;Password=postgres;" };
            AuditLogger L = new AuditLogger();

            for (int i=0; i < 10; i++)
            {
                object DetailObj = new
                {
                    // Arbitrary object structure is supported.  Typed class instances should be fine too.  
                    String1 = "xyz",  // serializes as string
                    Array1 = new int[] { 1, 2, 3 },  // serializes as array of number
                    Object1 = new  // serializes as object of:
                    {
                        Subfield1 = 11,  // serializes as number
                        Subfield2 = "Value2",  // serializes as string
                        Subfield3 = 4.56,  // serializes as number
                        ArrayOfChar1 = new char[] { 'a', 'b', 'c' },  // serializes as string array
                        ArrayOfString1 = new string[] { "a", "b", "c" },  // serializes as string array
                        ListOfString1 = new List<string>( new string[] { "a", "b", "c" } ), // serializes as string array
                    }
                };

                // DetailObj is serialized and then persisted as jsonb.  Arbitrary structure is supported, including typed or anonymous objects.
                // L.Log(AuditEvent.New("AuditLogLibTest", "Incorrect password provided by user", AuditEventType.LoginFailure, DetailObj, "Bad@there.com", new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11).ToString()));
                // L.Log(AuditEvent.New("AuditLogLibTest", "User logged in using biometric implant", AuditEventType.LoginSuccess, null, "Ok@here.com"));
                // L.Log(AuditEvent.New("AuditLogLibTest", "He came out of nowhere", AuditEventType.Unspecified, null, null));

                Thread.Sleep(2000);
            }
        }
    }
}
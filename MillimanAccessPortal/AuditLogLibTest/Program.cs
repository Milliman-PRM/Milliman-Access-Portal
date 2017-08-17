using System;
using System.Threading;
using AuditLogLib;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace AuditLogLibTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // This code demonstrates usage of the AuditLogger as a directly instantiated object, 
            // rather than as accessed through the ILoggerProvider interface implementation.  It works both ways. 

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
                // It is preferred to use AuditEvent.CreateNew() to enforce the list of user provided field values in the AuditEvent object that get used. 
                L.Log(LogLevel.Critical, AuditEventId.LoginFailure, AuditEvent.New("AuditLogLibTest", "Incorrect password provided by user", DetailObj, "Bad@there.com"), null, null);
                L.Log(LogLevel.Critical, AuditEventId.LoginSuccess, AuditEvent.New("AuditLogLibTest", "User logged in using biometric implant", null, "Ok@here.com"), null, null);
                L.Log(LogLevel.Critical, AuditEventId.Unspecified, AuditEvent.New("AuditLogLibTest", "He came out of nowhere", null, null), null, null);

                Thread.Sleep(2000);
            }
        }
    }
}
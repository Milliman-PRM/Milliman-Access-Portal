using System;
using System.Security.Cryptography;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MapCommonLib
{
    public static class GlobalFunctions
    {

        public static string emailValRegex { get; set; } = @"^(([^<>()[\]\\.,;:\s@']+(\.[^<>()[\]\\.,;:\s@']+)*)|('.+'))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$";
        public static string domainValRegex { get; set; } = @"^((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$";

        static Regex EmailAddressValidationRegex = new Regex (emailValRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

        public static bool IsValidEmail(string TestAddress)
        {
            try
            {
                return EmailAddressValidationRegex.IsMatch(TestAddress);
            }
            catch 
            {
                return false;
            }
        }

        public static string GetFileChecksum(string FilePath)
        {
            byte[] checksumBytes;
            using (Stream concatStream = System.IO.File.OpenRead(FilePath))
            using (HashAlgorithm hashAlgorithm = new SHA1Managed())
            {
                checksumBytes = hashAlgorithm.ComputeHash(concatStream);
            }
            string ChecksumString = BitConverter.ToString(checksumBytes).Replace("-", "");
            return ChecksumString;
            //var equal = resumableData.Checksum.Equals(checksum, StringComparison.OrdinalIgnoreCase)
        }

        public static string GetAssemblyCopyrightString(Assembly AssemblyArg)
        {
            // DateTime AssemblyLinkDateTime = GetLinkerTime(AssemblyArg);

            AssemblyCopyrightAttribute CopyrightAttribute = AssemblyArg.GetCustomAttribute<AssemblyCopyrightAttribute>();
            return CopyrightAttribute.Copyright;
        }

        /// <summary>
        /// Concatenates a caller provided message with the message of a provided exception, optionally recursing on every InnerException
        /// </summary>
        /// <param name="e"></param>
        /// <param name="LeadingLine"></param>
        /// <param name="RecurseInnerExceptions"></param>
        /// <returns></returns>
        public static string LoggableExceptionString(Exception e, string LeadingLine="Exception:", bool RecurseInnerExceptions=true, bool IncludeStackTrace = false)
        {
            string ErrMsg = LeadingLine;
            for (string Indent = "" ; e != null; e = e.InnerException)
            {
                ErrMsg += $"{Environment.NewLine}{Indent}{e.Message}";
                if (IncludeStackTrace && e.StackTrace != null)
                {
                    ErrMsg += $"{Environment.NewLine}{Regex.Replace(e.StackTrace, @" {2,}at ", $"{Indent}at ")}";
                }
                if (!RecurseInnerExceptions)
                {
                    break;
                }
                Indent += "  ";
            }
            return ErrMsg;
        }

        /// <summary>
        /// Gets the linker date of the assembly.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        /// <remarks>Requires disabling deterministic compile of the assembly (new default in VS 15.4). Use "<Deterministic>false</Deterministic>" in a PropertyGroup in the csproj</remarks>
        public static DateTime GetLinkerTime(Assembly assembly)
        {
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            return linkTimeUtc;
        }
    }
}

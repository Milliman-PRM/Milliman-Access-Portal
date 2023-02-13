using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip;

namespace MapCommonLib
{
    public static class GlobalFunctions
    {

        public static string EmailValRegex { get; set; } = @"^(([^<>()[\]\\.,;:\s@']+(\.[^<>()[\]\\.,;:\s@']+)*)|('.+'))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$";
        public static string DomainValRegex { get; set; } = @"^((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$";
        public static string FileDropValRegex { get; set; } = @"(\.{2})|(\/)|(\\)";
        public static ulong MaxFileUploadSize { get; set; } = 5368709120;
        public static ulong VirusScanWindowSeconds { get; set; } = 30;
        public static int DefaultClientDomainListCountLimit { get; set; } = 3;
        public static string MillimanSupportEmailAlias { get; set; } = "";
        public static List<string> NonLimitedDomains { get; set; } = new List<string> { "milliman.com" };
        public static List<string> ProhibitedDomains { get; set; } = new List<string> { "hotmail.com" };
        public static ConcurrentDictionary<string,DateTime> ContainerLastActivity { get; set; } = new ConcurrentDictionary<string,DateTime>();

        public static readonly int fallbackPasswordHistoryDays = 30;
        public static readonly int fallbackPasswordHashingIterations= 100_000;
        public static readonly int fallbackAccountActivationTokenTimespanDays = 7;
        public static readonly int fallbackPasswordResetTokenTimespanHours = 4;

        public static readonly string PasswordResetTokenProviderName = "MAPResetToken";
        public static readonly string TwoFactorEmailTokenProviderName = "TwoFactorEmailTokenProvider";
        public static readonly string DefaultPgEscapeChar = @"\";

        static Regex EmailAddressValidationRegex = new Regex (EmailValRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

        public static UriBuilder MapUriRoot { get; set; }

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

        static Regex FileDropValidationRegex = new Regex(FileDropValRegex, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

        public static bool isValidFileDropItemName(string TestValue)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(TestValue) &&
                       !FileDropValidationRegex.IsMatch(TestValue) &&
                       !TestValue.Any(v => Path.GetInvalidFileNameChars().Contains(v));
            }
            catch
            {
                return false;
            }
        }

        public static (string checksum, long length) GetFileChecksum(string FilePath)
        {
            using (Stream concatStream = File.OpenRead(FilePath))
            using (HashAlgorithm hashAlgorithm = new SHA1Managed())
            {
                byte[] checksumBytes = hashAlgorithm.ComputeHash(concatStream);
                return (BitConverter.ToString(checksumBytes).Replace("-", ""), concatStream.Length);
            }
        }

        public static string GetStringChecksum(string Arg)
        {
            using (HashAlgorithm hashAlgorithm = new SHA1Managed())
            {
                byte[] checksumBytes = hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(Arg));
                return BitConverter.ToString(checksumBytes).Replace("-", "");
            }
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
        public static string LoggableExceptionString(Exception e, string LeadingLine="Exception:", bool RecurseInnerExceptions=true, bool IncludeStackTrace = false, bool UseHtmlBR = false)
        {
            string ErrMsg = LeadingLine;
            for (string Indent = "" ; e != null; e = e.InnerException)
            {
                string Break = UseHtmlBR ? "<BR>" : Environment.NewLine;

                ErrMsg += $"{Break}{Indent}{e.Message}";
                if (IncludeStackTrace && e.StackTrace != null)
                {
                    ErrMsg += $"{Break}{Regex.Replace(e.StackTrace, @" {2,}at ", $"{Indent}at ")}";
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

        public static string GenerateErrorMessage(IConfiguration configuration, string subject)
        {
            string messageTemplate = configuration.GetSection("Global")["ErrorMessageTemplate"];
            return string.Format(messageTemplate, subject);
        }

        /// <summary>
        /// Intended for internal use by DoesEmailSatisfyClientWhitelists method
        /// </summary>
        public class TrimCaseInsensitiveStringComparer : IEqualityComparer<string>
        {
            public bool Equals(string l, string r)
            {
                if (ReferenceEquals(l, r)) return true;
                if (l is null || r is null) return false;
                return l.Trim().Equals(r.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            public int GetHashCode(string Arg)
            {
                return Arg.Trim().ToLower().GetHashCode();
            }
        };
        /// <summary>
        /// All argument values are trimmed by the value comparer so need not be trimmed before calling
        /// </summary>
        /// <param name="email"></param>
        /// <param name="domains"></param>
        /// <param name="addresses"></param>
        /// <returns></returns>
        public static bool DoesEmailSatisfyClientWhitelists(string email, IEnumerable<string> domains, IEnumerable<string> addresses)
        {
            IEqualityComparer<string> comparer = new TrimCaseInsensitiveStringComparer();

            return domains.Contains(email.Substring(email.IndexOf('@') + 1), comparer) || addresses.Contains(email, comparer);
        }

        public static void IssueLog(IssueLogEnum issue, string message, LogEventLevel level = LogEventLevel.Information, params object[] parms)
        {
            Log.Write(level, $"{issue.GetDisplayNameString()}: {message}", parms);
        }

        public static void IssueLog(IssueLogEnum issue, Exception ex, string message, LogEventLevel level = LogEventLevel.Information, params object[] parms)
        {
            Log.Write(level, ex, $"{issue.GetDisplayNameString()}: {message}", parms);
        }

        public static string UtcToLocalString(DateTime dateTime, string timeZoneId)
        {
            TimeZoneInfo requestedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            dateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, requestedTimeZone);
            string timeZoneString = requestedTimeZone.IsDaylightSavingTime(dateTime)
                ? requestedTimeZone.DaylightName
                : requestedTimeZone.StandardName;

            return dateTime.ToString($"ddd, dd MMM yyyy hh':'mm tt', {timeZoneString}'");
        }

        public static string UtcToSortableDateString(DateTime dateTime, string timeZoneId)
        {
            TimeZoneInfo requestedTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            dateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, requestedTimeZone);

            return dateTime.ToString("s");
        }

        /// <summary>
        /// Note that the escape character used in this function is @"\"
        /// </summary>
        /// <param name="original">The string to process</param>
        /// <returns></returns>
        public static string EscapePgWildcards(string original)
        {
            return EscapePgWildcards(original, DefaultPgEscapeChar);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="original">The string to process</param>
        /// <param name="escapeChar">The escape character to prepend to any character in `original` that is intended literally in the search pattern</param>
        /// <returns></returns>
        public static string EscapePgWildcards(string original, string escapeChar)
        {
            if (escapeChar.Length != 1)
            {
                throw new ArgumentException("Invalid escape string", nameof(escapeChar));
            }

            return original.Replace(escapeChar, escapeChar + escapeChar)
                           .Replace("_", escapeChar + "_")
                           .Replace("%", escapeChar + "%");
        }

        /// <summary>
        /// Returns an MD5 hash of the input source byte array
        /// </summary>
        /// <param name="source">array of bytes to be hashed</param>
        /// <param name="forceCase">true for upper case, false for lower case, null for no intervention</param>
        /// <returns></returns>
        public static string HexMd5String(byte[] source, bool? forceCase = null)
        {
#if NETSTANDARD
            byte[] hashBytes = MD5.Create().ComputeHash(source);
            string returnVal = BitConverter.ToString(hashBytes).Replace("-", "");
#else
            // These lines are better but the methods are not supported in NETSTANDARD.  Eventually all our libraries should target .net 5+
            byte[] hashBytes = MD5.HashData(source);
            string returnVal = Convert.ToHexString(hashBytes);
#endif
            if (forceCase.HasValue)
            {
                returnVal = forceCase.Value switch
                {
                    true => returnVal.ToUpper(),
                    false => returnVal.ToLower()
                };
            }

            return returnVal;
        }

        /// <summary>
        /// Returns an MD5 hash of the input source byte array
        /// </summary>
        /// <param name="source">array of bytes to be hashed</param>
        /// <param name="forceCase">true for upper case, false for lower case, null for no intervention</param>
        /// <returns></returns>
        public static string HexMd5String(Guid guid, bool? forceCase = null)
        {
            return HexMd5String(guid.ToByteArray(), forceCase);    
        }

        /// <summary>
        /// Extracts all contents of a compressed file to a folder.  The source file is not deleted. 
        /// A tar file should use only ASCII encoding in the name fields.
        /// </summary>
        /// <param name="fileFullPath">Filename extension must be .tar or .gz</param>
        /// <param name="targetFolder">If not provided, the contents will be extracted to the folder containing the tar file</param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void ExtractCompressedFile(string fileFullPath, string targetFolder = null)
        {
            if (!File.Exists(fileFullPath))
            {
                throw new FileNotFoundException("Unable to extract archive, not found", fileFullPath);
            }

            targetFolder = targetFolder ?? Path.GetDirectoryName(fileFullPath);

            if (!Directory.Exists(targetFolder))
            {
                throw new DirectoryNotFoundException($"Unable to extract archive to folder, target folder <{targetFolder}> not found");
            }

            using (Stream rawFileStream = File.OpenRead(fileFullPath))
            {
                switch (fileFullPath)
                {
                    case string name when name.EndsWith(".tar", StringComparison.InvariantCultureIgnoreCase):
                        using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(rawFileStream, null)) // If name encoding changes, note this in the method comment
                        {
                            tarArchive.ExtractContents(targetFolder);
                        }
                        break;

                    case string name when name.EndsWith(".gz", StringComparison.InvariantCultureIgnoreCase):
                        using (Stream gzipStream = new GZipInputStream(rawFileStream))
                        {
                            using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream, null)) // If name encoding changes, note this in the method comment
                            {
                                tarArchive.ExtractContents(targetFolder);
                            }
                        }
                        break;

                    case string name when name.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase):
                        using (ZipInputStream zipStream = new ZipInputStream(rawFileStream))
                        {
                            ZipEntry theEntry;
                            while ((theEntry = zipStream.GetNextEntry()) != null)
                            {

                                Console.WriteLine(theEntry.Name);

                                string directoryName = Path.GetDirectoryName(theEntry.Name);
                                string fileName = Path.GetFileName(theEntry.Name);

                                string destinationFile = Path.Combine(targetFolder, fileName.TrimStart('/'));

                                // create directory
                                if (directoryName.Length > 0)
                                {
                                    Directory.CreateDirectory(directoryName);
                                }

                                if (fileName != String.Empty)
                                {
                                    using (FileStream streamWriter = File.Create(destinationFile))
                                    {

                                        int size = 2048;
                                        byte[] data = new byte[2048];
                                        while (true)
                                        {
                                            size = zipStream.Read(data, 0, data.Length);
                                            if (size > 0)
                                            {
                                                streamWriter.Write(data, 0, size);
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    default:
                        throw new ArgumentException($"Archive file name {Path.GetFileName(fileFullPath)} does not have a supported extension", nameof(fileFullPath));
                }
            }
        }
    }

    /// <summary>
    /// This enum helps to quickly find/remove code associated with an issue after the issue is resolved. 
    /// Remove the enum value and the compiler will highlight all references. 
    /// </summary>
    public enum IssueLogEnum
    {
        LongRunningSelectionGroupProcessing,
        TrackQlikviewApiTiming,
        TrackingContainerPublishing,
        ContainerLaunchFlow,
    }

}

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Milliman.Common {
    public class SysInfo {
        public class MemoryStatus {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool GlobalMemoryStatusEx([In] [Out] MemoryStatusEx buffer);

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            private class MemoryStatusEx {
                public uint dwLength, dwMemoryLoad;

                public ulong TotalPhys, AvailPhys, TotalPageFile, AvailPageFile, TotalVirtual, AvailVirtual, AvailExtendedVirtual;

                public MemoryStatusEx() { dwLength = (uint) Marshal.SizeOf(typeof(MemoryStatusEx)); }
            }

            private readonly MemoryStatusEx _memory_status_ex;

            public ulong TotalPhysical {get {return _memory_status_ex.TotalPhys;}}

            public ulong AvaliablePhysical {get {return _memory_status_ex.AvailPhys;}}

            private ulong TotalPageFile {get {return _memory_status_ex.TotalPageFile;}}

            private ulong AvailablePageFile {get {return _memory_status_ex.AvailPageFile;}}

            public MemoryStatus() {
                _memory_status_ex = new MemoryStatusEx();
                try {
                    GlobalMemoryStatusEx(this._memory_status_ex);
                } catch( Exception ) {
                }
            }

            public override string ToString() {
                string text_format = "Available Memory={0} | Total Memory = {1} | Available PageFile = {2} | Total PageFile = {3}"; ;
                return string.Format(text_format,
                     FormatMb(AvaliablePhysical),
                     FormatMb(TotalPhysical),
                     FormatMb(AvailablePageFile),
                     FormatMb(TotalPageFile));
            }
        }

        public enum EnumOperatingSystem {
            x64,
            x86
        }

        public StringWriter GetSystemInformation(Assembly assembly) {
            StringWriter stringWriter = new StringWriter();
            int num = 0;
            foreach( KeyValuePair<string, Dictionary<string, string>> current in this.GetSystemInformation2(assembly) ) {
                if( num++ > 0 ) 
                    stringWriter.WriteLine("");
                stringWriter.WriteLine(current.Key);
                stringWriter.WriteLine(new string('=', current.Key.Length));
                foreach( KeyValuePair<string, string> current2 in current.Value ) {
                    stringWriter.WriteLine(current2.Key + ": " + current2.Value);
                }
            }
            return stringWriter;
        }

        public Dictionary<string, Dictionary<string, string>> GetSystemInformation2(Assembly assembly) {
            return new Dictionary<string, Dictionary<string, string>>
            {
                {"Product Information",GetProductInformation(assembly)},
                {"Current Process Information",GetCurrentProcessInformation()},
                {"Machine Information",GetMachineInformation()},
                {"CPU Information",GetCPUInformation()},
                {"Logical Drives Information",GetLogicalDrivesInformation()},
                {"Network Information",GetNetworkInformation()},
                {"File Information",GetFilesInformation()}
            };
        }

        private Dictionary<string, string> GetProductInformation(Assembly assembly) {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            object[] customAttributes = assembly.ManifestModule.Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            dictionary.Add("Product name", ((AssemblyTitleAttribute) customAttributes[0]).Title);
            dictionary.Add("Client Build Number", assembly.GetName().Version.ToString());
            dictionary.Add("Target Platform", "");
            return dictionary;
        }

        private Dictionary<string, string> GetMachineInformation() {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            //dictionary.Add("Computer Name", System.Windows.Forms.SystemInformation.ComputerName);
            dictionary.Add("Operating System Version", string.Concat(new object[]
            {
                DeterminateOSVersion(),
                " ",
                GetOSArchitecture(),
                " (",
                Environment.OSVersion,
                ")"
            }));
            dictionary.Add(".NET Version", Environment.Version.ToString());
            dictionary.Add("MDAC Version", Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\DataAccess\\").GetValue("FullInstallVer").ToString());
            //dictionary.Add("Monitors", System.Windows.Forms.SystemInformation.MonitorCount.ToString());
            MemoryStatus memoryStatus = new MemoryStatus();
            dictionary.Add("Physical Memory", memoryStatus.TotalPhysical / 1024uL / 1024uL + "Mb");
            dictionary.Add("Available Memory", memoryStatus.AvaliablePhysical / 1024uL / 1024uL + "Mb");
            return dictionary;
        }

        private Dictionary<string, string> GetCPUInformation() {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            try {
                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor");
                if( registryKey != null ) {
                    string[] subKeyNames = registryKey.GetSubKeyNames();
                    for( int i = 0; i < subKeyNames.Length; i++ ) {
                        string text = subKeyNames[i];
                        try {
                            dictionary.Add("CPU " + text, registryKey.OpenSubKey(text).GetValue("ProcessorNameString").ToString());
                        } catch( Exception ) {
                            dictionary.Add("CPU " + text, registryKey.OpenSubKey(text).GetValue("Identifier").ToString());
                        }
                    }
                }
            } catch( Exception ) {
            }
            return dictionary;
        }

        private Dictionary<string, string> GetCurrentProcessInformation() {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            Process currentProcess = Process.GetCurrentProcess();
            //dictionary.Add("User Domain Name", System.Windows.Forms.SystemInformation.UserDomainName);
            //dictionary.Add("User Name", System.Windows.Forms.SystemInformation.UserName);
            dictionary.Add("Start Directory", Path.GetDirectoryName(currentProcess.MainModule.FileName));
            dictionary.Add("File Name", Path.GetFileName(currentProcess.MainModule.FileName));
            dictionary.Add("Process ID", currentProcess.Id.ToString());
            dictionary.Add("Base Priority", currentProcess.BasePriority.ToString());
            dictionary.Add("Processor Affinity", currentProcess.ProcessorAffinity.ToString());
            dictionary.Add("Privileged Processor Time", currentProcess.PrivilegedProcessorTime.ToString());
            //dictionary.Add("Input Language", string.Concat(new object[]
            //{
            //    InputLanguage.CurrentInputLanguage.LayoutName,
            //    " (",
            //    InputLanguage.CurrentInputLanguage.Culture,
            //    ")"
            //}));
            dictionary.Add("Working Set", currentProcess.WorkingSet64.ToString());
            dictionary.Add("Minimum Working Set", currentProcess.MinWorkingSet.ToString());
            dictionary.Add("Maximum Working Set", currentProcess.MaxWorkingSet.ToString());
            return dictionary;
        }

        private Dictionary<string, string> GetLogicalDrivesInformation() {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            StringWriter stringWriter = new StringWriter();
            string[] logicalDrives = Directory.GetLogicalDrives();
            for( int i = 0; i < logicalDrives.Length; i++ ) {
                string str = logicalDrives[i];
                stringWriter.Write(str + " ");
            }
            dictionary.Add("Drives", stringWriter.ToString());
            return dictionary;
        }

        private Dictionary<string, string> GetNetworkInformation() {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("Host Name", Dns.GetHostName());
            int num = 0;
            IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            for( int i = 0; i < addressList.Length; i++ ) {
                IPAddress iPAddress = addressList[i];
                dictionary.Add("IP Address " + ++num, iPAddress.ToString());
            }
            return dictionary;
        }

        private Dictionary<string, string> GetFilesInformation() {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            Process currentProcess = Process.GetCurrentProcess();
            string[] files = Directory.GetFiles(Path.GetDirectoryName(currentProcess.MainModule.FileName));
            for( int i = 0; i < files.Length; i++ ) {
                string text = files[i];
                try {
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(text);
                    dictionary.Add(Path.GetFileName(text), string.Concat(new object[]
                    {
                        "Date=",
                        File.GetLastWriteTime(text),
                        ", Version=",
                        versionInfo.FileVersion
                    }));
                } catch {
                }
            }
            return dictionary;
        }

        private string DeterminateOSVersion() {
            string result = "";
            try {
              // Implement "HOW TO GET OS VERSION... 
            } catch {
                result = "Error failed to get OS-Version";
            }
            return result;
        }

        public static string FormatMemoryText(long num) {
            double kb = 1024, mb = kb * 1024, gb = mb * 1024, tb = gb * 1024;
            if( num / kb < 1  ) {
                return num + " bytes";
            } else if( num / mb  < 1) {
                return (num / kb) + " KB";
            } else if( num / gb < 1) {
                return (num / mb) + " MB";
            } else if( num / tb < 1) {
                return num / gb + " GB";
            } else
                return num / tb + " TB";
        }

        public static string FormatMb(ulong num) {
            double kb = 1024, mb = kb * 1024, gb = mb * 1024, tb = gb * 1024;
            if( num / kb < 1 ) {
                return num + " bytes";
            } else if( num / mb < 1 ) {
                return (num / kb) + " KB";
            } else if( num / gb < 1 ) {
                return (num / mb) + " MB";
            } else if( num / tb < 1 ) {
                return num / gb + " GB";
            } else
                return num / tb + " TB";
        }

        public EnumOperatingSystem GetOSArchitecture() {
            if( IntPtr.Size == 4 ) {
                return EnumOperatingSystem.x86;
            }
            return EnumOperatingSystem.x64;
        }
    }
}
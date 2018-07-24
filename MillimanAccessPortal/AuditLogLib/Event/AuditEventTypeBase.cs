using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace AuditLogLib.Event
{
    public abstract class AuditEventTypeBase
    {
        private static string _baseName = null;
        private static string BaseName
        {
            get
            {
                if (_baseName == null)
                {
                    _baseName = GetBaseName();
                }
                return _baseName;
            }
        }

        protected readonly int id;
        protected readonly string name;

        private static string GetBaseName([CallerFilePath] string callerPath = "")
        {
            var depth = 4;  // based on this file's location within the repository
            for (var i = 0; i < depth; i += 1)
            {
                callerPath = Path.GetDirectoryName(callerPath);
            }
            return callerPath;
        }

        public AuditEventTypeBase(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        protected AuditEvent ToEvent(string callerName, string callerPath, int callerLine)
        {
            // Path.GetRelativePath is not available in .NET Standard 2.0
            // Use a naive approach instead that works for the common case
            var relativeCallerPath = callerPath.Replace(BaseName, "");

            return new AuditEvent
            {
                TimeStampUtc = DateTime.UtcNow,
                EventType = name,
                Source = $"{relativeCallerPath}:{callerLine} {callerName}",
            };
        }

        public override string ToString()
        {
            return $"{id}:{name}";
        }
    }
}

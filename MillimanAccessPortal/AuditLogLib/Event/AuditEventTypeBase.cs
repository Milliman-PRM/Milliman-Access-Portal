using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace AuditLogLib.Event
{
    public abstract class AuditEventTypeBase
    {
        private static string _pathToRemove = null;

        /// <summary>
        /// Initializes the root portion of the path that should be removed from all log message caller paths.  Call from code one folder below the solution
        /// </summary>
        /// </summary>
        /// <param name="LevelsUp">How many folders up from location of the calling code should be the root to be removed</param>
        /// <param name="callerFilePath">Normally no value should be provided</param>
        public static void SetPathToRemove(int LevelsUp = 1, [CallerFilePath] string callerFilePath = "")
        {
            if (string.IsNullOrWhiteSpace(callerFilePath))
            {
                _pathToRemove = null;
            }
            else
            {
                for (int index = 0; index <= LevelsUp; index++)
                {
                    callerFilePath = Path.GetDirectoryName(callerFilePath);
                }
                _pathToRemove = callerFilePath;
            }
        }

        protected readonly int id;
        protected readonly string name;

        public AuditEventTypeBase(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        protected AuditEvent ToEvent(string callerName, string callerPath, int callerLine)
        {
            // Path.GetRelativePath() is not available in .NET Standard 2.0
            if (!string.IsNullOrWhiteSpace(_pathToRemove))
            {
                callerPath.Replace(_pathToRemove, ".");
            }

            return new AuditEvent
            {
                TimeStampUtc = DateTime.UtcNow,
                EventType = name,
                Source = $"{callerPath}:{callerLine} {callerName}",
            };
        }

        public override string ToString()
        {
            return $"{id}:{name}";
        }
    }
}

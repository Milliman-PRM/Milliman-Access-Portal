using System.IO;
using System.Threading;

namespace MapCommonLib
{
    public class FileSystemUtil
    {
        /// <summary>
        /// Try to delete a file until success
        /// </summary>
        /// <remarks>Time before retry increases after each attempt.</remarks>
        /// <param name="path">Path to the file to be deleted</param>
        /// <param name="attempts">Times to try deleting the directory before giving up</param>
        /// <param name="baseIntervalMs">Time to wait after initial attempt</param>
        public static void DeleteFileWithRetry(string path, int attempts = 5, int baseIntervalMs = 500)
        {
            IOOperationWithRetry(File.Delete, path, attempts, baseIntervalMs);
        }

        /// <summary>
        /// Try to delete a directory until success
        /// </summary>
        /// <remarks>Time before retry increases after each attempt.</remarks>
        /// <param name="path">Path to the directory to be deleted</param>
        /// <param name="attempts">Times to try deleting the directory before giving up</param>
        /// <param name="baseIntervalMs">Time to wait after initial attempt</param>
        public static void DeleteDirectoryWithRetry(string path, int attempts = 5, int baseIntervalMs = 500)
        {
            IOOperationWithRetry(p => Directory.Delete(p, true), path, attempts, baseIntervalMs);
        }

        /// <summary>
        /// Represents an operation that can throw an IOException
        /// </summary>
        /// <param name="opArg">Arbitrary argument. Often a path to a file or directory.</param>
        private delegate void IOOperation(string opArg);

        /// <summary>
        /// Try to perform an IO operation until it succeeds
        /// </summary>
        /// <remarks>
        /// Time before retry increases after each attempt. Retry intervals form a triangular sequence.
        /// </remarks>
        /// <param name="operation">IO operation to try until success. Only retried when IOException is thrown.</param>
        /// <param name="opArg">Passed to operation delegate</param>
        /// <param name="attempts">Times to try the operation before giving up</param>
        /// <param name="baseIntervalMs">Time to wait after initial attempt</param>
        private static void IOOperationWithRetry(IOOperation operation, string opArg, int attempts, int baseIntervalMs)
        {
            int retryInterval = 0;
            int attemptNo = 0;

            while (true)
            {
                try
                {
                    operation(opArg);
                    break;
                }
                catch (IOException)
                {
                    attemptNo += 1;

                    int attemptsLeft = attempts - attemptNo;
                    if (attemptsLeft == 0)
                    {
                        throw;
                    }
                    GlobalFunctions.TraceWriteLine(
                        $"Failed to delete directory '{opArg}'; retrying {attemptsLeft} more times...");

                    retryInterval += attemptNo;
                    Thread.Sleep(retryInterval * baseIntervalMs);
                }
            }
        }
    }
}

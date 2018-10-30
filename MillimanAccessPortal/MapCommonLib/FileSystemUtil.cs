using System;
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
            ApplyRetryOperation<IOException, string>(File.Delete, attempts, baseIntervalMs, path);
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
            ApplyRetryOperation<IOException, string>(p => Directory.Delete(p, true), attempts, baseIntervalMs, path);
        }

        /// <summary>
        /// Represents a retriable operation that throws an exception when it should be retried
        /// </summary>
        /// <param name="opArg"></param>
        private delegate void RetryOperation<TException, TArg>(TArg opArg) where TException : Exception;

        /// <summary>
        /// Try to perform a retriable operation until it succeeds.
        /// Time before retry increases after each attempt. Retry intervals form a triangular sequence.
        /// </summary>
        /// <typeparam name="TException">
        /// Exception type to monitor. When operation throws an exception of this type, it will be retried.
        /// </typeparam>
        /// <typeparam name="TArg"></typeparam>
        /// <param name="operation">
        /// Retriable operation to try until success. Only retried when TException is thrown.
        /// </param>
        /// <param name="attempts">Times to try the operation before giving up</param>
        /// <param name="baseIntervalMs">Time to wait after initial attempt</param>
        /// <param name="opArg">Passed to operation delegate</param>
        /// <summary>
        private static void ApplyRetryOperation<TException, TArg>(
            RetryOperation<TException, TArg> operation, int attempts, int baseIntervalMs, TArg opArg)
            where TException : Exception
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
                catch (TException)
                {
                    attemptNo += 1;

                    int attemptsLeft = attempts - attemptNo;
                    if (attemptsLeft == 0)
                    {
                        throw;
                    }
                    GlobalFunctions.TraceWriteLine(
                        $"Failed to apply retry operation with argument '{opArg}'; "
                        + $"retrying {attemptsLeft} more times...");

                    retryInterval += attemptNo;
                    Thread.Sleep(retryInterval * baseIntervalMs);
                }
            }
        }
    }
}

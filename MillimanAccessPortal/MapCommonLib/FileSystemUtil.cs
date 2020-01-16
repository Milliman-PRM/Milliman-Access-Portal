using System.IO;

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
        public static void DeleteFileWithRetry(string path, int attempts = 5, int baseIntervalMs = 200)
        {
            StaticUtil.ApplyRetryOperation<IOException, string>(File.Delete, attempts, baseIntervalMs, path);
        }

        /// <summary>
        /// Try to delete a directory until success
        /// </summary>
        /// <remarks>Time before retry increases after each attempt.</remarks>
        /// <param name="path">Path to the directory to be deleted</param>
        /// <param name="attempts">Times to try deleting the directory before giving up</param>
        /// <param name="baseIntervalMs">Time to wait after initial attempt</param>
        public static void DeleteDirectoryWithRetry(string path, int attempts = 5, int baseIntervalMs = 200)
        {
            StaticUtil.ApplyRetryOperation<IOException, string>(p => Directory.Delete(p, true), attempts, baseIntervalMs, path);
        }

        /// <summary>
        /// Try to copy a file until success
        /// </summary>
        /// <remarks>Time before retry increases after each attempt.</remarks>
        /// <param name="source">The source path</param>
        /// <param name="destination">The destination path</param>
        /// <param name="attempts">Times to try deleting the directory before giving up</param>
        /// <param name="baseIntervalMs">Time to wait after initial attempt</param>
        public static void CopyFileWithRetry(string source, string destination, int attempts = 3, int baseIntervalMs = 200)
        {
            StaticUtil.ApplyRetryOperation<IOException, string, string>(File.Copy, attempts, baseIntervalMs, source, destination);
        }

    }
}

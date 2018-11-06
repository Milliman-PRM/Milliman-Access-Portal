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
        public static void DeleteFileWithRetry(string path, int attempts = 5, int baseIntervalMs = 500)
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
        public static void DeleteDirectoryWithRetry(string path, int attempts = 5, int baseIntervalMs = 500)
        {
            StaticUtil.ApplyRetryOperation<IOException, string>(p => Directory.Delete(p, true), attempts, baseIntervalMs, path);
        }
    }
}

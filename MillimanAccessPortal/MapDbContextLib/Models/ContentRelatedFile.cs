/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib;

namespace MapDbContextLib.Models
{
    public class ContentRelatedFile
    {
        private string _Checksum = null;

        public string FullPath { get; set; }

        /// <summary>
        /// Standard values: MasterContent, ReducedContent, UserGuide, Thumbnail, ReleaseNotes
        /// </summary>
        public string FilePurpose { get; set; }
        public string FileOriginalName { get; set; }
        public string SortOrder { get; set; } = "";

        public string Checksum
        {
            get { return _Checksum; }
            set { _Checksum = value?.ToLower(); }
        }

        /// <summary>
        /// Validates the bytes of a file agains the stored checksum property of this ContentRelatedFile instance
        /// </summary>
        /// <returns>true if the stored checksum matches the file content</returns>
        public bool ValidateChecksum()
        {
            return Checksum == GlobalFunctions.GetFileChecksum(FullPath).ToLower();
        }

        /// <summary>
        /// Exists because null and empty are semantically equivalent
        /// </summary>
        /// <param name="testSortOrder"></param>
        /// <returns></returns>
        public bool SequenceOrderMatches(string testSortOrder)
        {
            if (string.IsNullOrEmpty(SortOrder) && 
                string.IsNullOrEmpty(testSortOrder))
            {
                return true;
            }
            return SortOrder == testSortOrder;
        }
    }
}

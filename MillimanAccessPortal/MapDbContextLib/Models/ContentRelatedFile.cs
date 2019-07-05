/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Represents properties of a file that is related to a MAP content item
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib;
using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace MapDbContextLib.Models
{
    public class ContentRelatedFile
    {
        [ReadOnly(true)]
        public Guid Id { get; set; } // = Guid.NewGuid();

        public string FullPath { get; set; }

        /// <summary>
        /// Standard values: MasterContent, ReducedContent, UserGuide, Thumbnail, ReleaseNotes
        /// </summary>
        public string FilePurpose { get; set; }

        public string FileOriginalName { get; set; }

        public ContentRelatedFileType FileType { get; set; } = ContentRelatedFileType.Unknown;

        public string SortOrder { get; set; } = "";

        private string _Checksum = null;
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
        public bool SortOrderMatches(string testSortOrder)
        {
            if (string.IsNullOrEmpty(SortOrder) && 
                string.IsNullOrEmpty(testSortOrder))
            {
                return true;
            }
            return SortOrder.Equals(testSortOrder, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class ContentRelatedFileIdComparer : IEqualityComparer<ContentRelatedFile>
    {
        public bool Equals(ContentRelatedFile crf1, ContentRelatedFile crf2)
        {
            if (crf1 == null || crf2 == null)
                return false;
            else return crf1.Id.Equals(crf2.Id);
        }

        public int GetHashCode(ContentRelatedFile crf)
        {
            return crf.Id.GetHashCode();
        }
    }
}

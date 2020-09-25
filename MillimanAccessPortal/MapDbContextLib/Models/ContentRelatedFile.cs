/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

namespace MapDbContextLib.Models
{
    public class ContentRelatedFile : VerifyableFileBase
    {
        /// <summary>
        /// Standard values: MasterContent, ReducedContent, UserGuide, Thumbnail, ReleaseNotes
        /// </summary>
        public string FilePurpose { get; set; }
        public string FileOriginalName { get; set; }
        public string Checksum
        {
            get { return _Checksum; }
            set { _Checksum = value; }
        }
        public string FullPath
        {
            get { return _FullPath; }
            set { _FullPath = value; }
        }

    }
}

using MapCommonLib;

namespace MapDbContextLib.Models
{
    public class ContentRelatedFile
    {
        private string _Checksum = null;

        public string FullPath { get; set; }
        public string FilePurpose { get; set; }
        public string FileOriginalName { get; set; }
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
    }
}

using System;

namespace PowerBiMigration
{
    public class ProcessedItem
    {
        public Guid ClientId { get; set; }
        
        public Guid ContentItemId { get; set; }

        public string OldGroupId { get; set; }
        
        public string NewGroupId { get; set; }
        
        public string OldReportId { get; set; }

        public string NewReportId { get; set; }

        public string ReportName { get; set; }
        
        public ProcessingStatus Status { get; set; }

        public TimeSpan ExportTime { get; set; }

        public TimeSpan ImportTime { get; set; }
    }

    public enum ProcessingStatus
    {
        ExportSuccess,
        FileSaveSuccess,
        ImportSuccess,
        DbUpdateSuccess,
        ExportFail,
        FileSaveFail,
        ImportFail,
        DbUpdateFail,
    }
}

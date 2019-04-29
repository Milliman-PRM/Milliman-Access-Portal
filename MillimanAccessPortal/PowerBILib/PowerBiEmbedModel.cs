/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: View model representing values used to view a PowerBi embedded report
 * DEVELOPER NOTES: <What future developers need to know.>
 */


namespace PowerBiLib
{
    public class PowerBiEmbedModel
    {
        public string ReportId { get; set; }
        public string EmbedToken { get; set; }
        public string EmbedUrl { get; set; }
    }
}

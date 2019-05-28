/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A family of model classes representing root content item properties that are particular to the associated content type
 * DEVELOPER NOTES: <What future developers need to know.>
 */

namespace MapDbContextLib.Models
{
    public class TypeSpecificContentItemProperties
    {}

    public class PowerBiContentItemProperties : TypeSpecificContentItemProperties
    {
        public bool FilterPaneEnabled { get; set; }

        public bool NavigationPaneEnabled { get; set; }

        public string PreviewWorkspaceId { get; set; }

        public string PreviewReportId { get; set; }

        public string PreviewEmbedUrl { get; set; }

        public string LiveWorkspaceId { get; set; }

        public string LiveReportId { get; set; }

        public string LiveEmbedUrl { get; set; }
    }
}

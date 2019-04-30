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

        public PowerBiEmbedModel LiveEmbedProperties { get; set; }

        public PowerBiEmbedModel PreviewEmbedProperties { get; set; }

    }

    public class PowerBiEmbedModel
    {
        public string ReportId { get; set; }

        public string EmbedToken { get; set; }

        public string EmbedUrl { get; set; }
    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A family of model classes representing root content item properties that are particular to the associated content type
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using System;

namespace MapDbContextLib.Models
{
    public class TypeSpecificContentItemProperties
    {}

    public class PowerBiContentItemProperties : TypeSpecificContentItemProperties
    {
        public bool EditableEnabled { get; set; }

        public bool FilterPaneEnabled { get; set; }

        public bool NavigationPaneEnabled { get; set; }

        public bool BookmarksPaneEnabled { get; set; }

        public Guid? PreviewWorkspaceId { get; set; }

        public Guid? PreviewReportId { get; set; }

        public string PreviewEmbedUrl { get; set; }

        public Guid? LiveWorkspaceId { get; set; }

        public Guid? LiveReportId { get; set; }

        public string LiveEmbedUrl { get; set; }
    }

    public class ContainerizedAppContentItemProperties : TypeSpecificContentItemProperties
    {
        // TODO Add whatever is needed to get a container launched for preview and live

        public string LiveImageName { get; set; }
        public ContainerCpuCoresEnum LiveContainerCpuCores { get; set; }
        public ContainerRamGbEnum LiveContainerRamGb { get; set; }
        public uint LiveContainerInternalPort { get; set; }

        public string PreviewImageName { get; set; }
        public ContainerCpuCoresEnum PreviewContainerCpuCores { get; set; }
        public ContainerRamGbEnum PreviewContainerRamGb { get; set; }
        public uint PreviewContainerInternalPort { get; set; }
    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A family of model classes representing root content item properties that are particular to the associated content type
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using System;
using System.Collections.Generic;

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
        public string LiveImageName { get; set; } = null;
        public string LiveImageTag { get; set; } = null;
        public ContainerCpuCoresEnum LiveContainerCpuCores { get; set; } = ContainerCpuCoresEnum.Unspecified;
        public ContainerRamGbEnum LiveContainerRamGb { get; set; } = ContainerRamGbEnum.Unspecified;
        public ushort LiveContainerInternalPort { get; set; } = 0;

        public string PreviewImageName { get; set; } = null;
        public string PreviewImageTag { get; set; } = null;
        public ContainerCpuCoresEnum PreviewContainerCpuCores { get; set; } = ContainerCpuCoresEnum.Unspecified;
        public ContainerRamGbEnum PreviewContainerRamGb { get; set; } = ContainerRamGbEnum.Unspecified;
        public ushort PreviewContainerInternalPort { get; set; } = 0;

        #region Lifetime management
        public bool AlwaysCold { get; set; } = true;
        public ContainerInstanceCooldownTimeEnum CooldownTime = ContainerInstanceCooldownTimeEnum.OneHour;
        public TimeSpan DailyStartTime { get; set; }
        public TimeSpan DailyStopTime { get; set; }
        public TimeZoneInfo ServiceTimeZone { get; set; }
        public List<DayOfWeek> ActiveServiceDaysOfWeek {get; set;}
        #endregion
    }
}

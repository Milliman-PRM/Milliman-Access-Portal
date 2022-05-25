/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A family of model classes representing root content item properties that are particular to the associated content type
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using MapDbContextLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

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
        public abstract class LifetimeSchemeBase
        {
            public ContainerInstanceLifetimeSchemeEnum Scheme;
        }

        public class AlwaysColdLifetimeScheme : LifetimeSchemeBase
        {
        }

        public class CustomScheduleLifetimeScheme : LifetimeSchemeBase
        {
            public bool IsScheduledOnNow()
            {
                TimeSpan utcNowTimeOfWeek = GetUtcTimeOfWeekForAnyDateTime(DateTime.UtcNow);

                switch (OrderedStateInstructionList)
                {
                    case var x when !x.Any():
                        return false;

                    case var x when utcNowTimeOfWeek < x.First().Key:
                        return x.Last().Value;

                    default:
                        return OrderedStateInstructionList.Aggregate(false, (accum, element) =>
                        {
                            return element.Key < utcNowTimeOfWeek
                                ? element.Value
                                : accum;
                        });
                }
            }

            public void AddScheduledStateInstruction(DateTime dateTime, bool turnOn)
            {
                RunStateInstructions.Add(GetUtcTimeOfWeekForAnyDateTime(dateTime), turnOn);
            }

            /// <summary>
            /// The key must be relative to the start of the week (Saturday night midnight) in UTC, zero offset.
            /// Sequential ordering is not required
            /// </summary>
            private Dictionary<TimeSpan, bool> RunStateInstructions { get; } = new Dictionary<TimeSpan, bool>();

            // A convenience property to guarantee the consumer a collection that is ordered by increasing time. 
            [JsonIgnore]
            private List<KeyValuePair<TimeSpan, bool>> OrderedStateInstructionList => RunStateInstructions.OrderBy(i => i.Key).ToList();

            private TimeSpan GetUtcTimeOfWeekForAnyDateTime(DateTime dateTime)
            {
                dateTime = dateTime.ToUniversalTime();
                DateTime weekStart = dateTime.Date.AddDays(-(int)dateTime.DayOfWeek);
                return dateTime - weekStart;
            }
        }


        public ContainerInstanceLifetimeSchemeEnum LifetimeScheme { get; set; }
        public ContainerCooldownPeriodEnum CooldownTime = ContainerCooldownPeriodEnum.OneHour;
        public TimeSpan DailyStartTimeUtc { get; set; }
        public TimeSpan DailyStopTimeUtc { get; set; }
        public TimeZoneInfo ServiceTimeZone { get; set; }
        public List<DayOfWeek> ActiveServiceDaysOfWeek {get; set;}
        #endregion
    }
}

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
        public LifetimeSchemeBase LiveContainerLifetimeScheme { get; set; } = null;

        public string PreviewImageName { get; set; } = null;
        public string PreviewImageTag { get; set; } = null;
        public ContainerCpuCoresEnum PreviewContainerCpuCores { get; set; } = ContainerCpuCoresEnum.Unspecified;
        public ContainerRamGbEnum PreviewContainerRamGb { get; set; } = ContainerRamGbEnum.Unspecified;
        public ushort PreviewContainerInternalPort { get; set; } = 0;

        #region Lifetime management
        public abstract class LifetimeSchemeBase
        {
            public ContainerInstanceLifetimeSchemeEnum Scheme { get; set; } = ContainerInstanceLifetimeSchemeEnum.Unspecified;
            public TimeSpan ContainerLingerTimeAfterActivity { get; set; }

            public LifetimeSchemeBase(ContainerizedContentPublicationProperties source)
            {
                Scheme = source.ContainerInstanceLifetimeScheme;
                ContainerLingerTimeAfterActivity = source.CustomCooldownPeriod switch
                {
                    ContainerCooldownPeriodEnum.ThirtyMinutes => TimeSpan.FromMinutes(30),
                    ContainerCooldownPeriodEnum.OneHour => TimeSpan.FromMinutes(60),
                    ContainerCooldownPeriodEnum.NinetyMinutes => TimeSpan.FromMinutes(90),
                    ContainerCooldownPeriodEnum.TwoHours => TimeSpan.FromMinutes(120),
                    _ => throw new NotImplementedException(),
                };
            }

        }

        public class AlwaysColdLifetimeScheme : LifetimeSchemeBase
        {
            public AlwaysColdLifetimeScheme(ContainerizedContentPublicationProperties source) 
                : base(source)
            {}
        }

        public class CustomScheduleLifetimeScheme : LifetimeSchemeBase
        {
            /// <summary>
            /// The key must be relative to the start of the week (Saturday night midnight) in UTC, zero offset.
            /// Sequential ordering is not required
            /// </summary>
            public Dictionary<TimeSpan, bool> RunStateInstructions { get; } = new Dictionary<TimeSpan, bool>();

            public CustomScheduleLifetimeScheme(ContainerizedContentPublicationProperties source)
                : base(source)
            {
                Action<bool?, DayOfWeek> AssignScheduledEventsForDay = (selected, dayOfWeek) =>
                {
                    if (selected.HasValue && selected.Value)
                    {
                        DateTime dateTimeOfWeekday = DateTime.Today + TimeSpan.FromDays(((int)dayOfWeek - (int)DateTime.Today.DayOfWeek) % 7);
                        if (source.StartTime.HasValue)
                        {
                            AddScheduledStateInstruction(dateTimeOfWeekday + source.StartTime.Value, true, source.TimeZoneId);
                        }
                        if (source.EndTime.HasValue)
                        {
                            AddScheduledStateInstruction(dateTimeOfWeekday + source.EndTime.Value, false, source.TimeZoneId);
                        }
                    }
                };

                AssignScheduledEventsForDay.Invoke(source.SundayChecked, DayOfWeek.Sunday);
                AssignScheduledEventsForDay.Invoke(source.MondayChecked, DayOfWeek.Monday);
                AssignScheduledEventsForDay.Invoke(source.TuesdayChecked, DayOfWeek.Tuesday);
                AssignScheduledEventsForDay.Invoke(source.WednesdayChecked, DayOfWeek.Wednesday);
                AssignScheduledEventsForDay.Invoke(source.ThursdayChecked, DayOfWeek.Thursday);
                AssignScheduledEventsForDay.Invoke(source.FridayChecked, DayOfWeek.Friday);
                AssignScheduledEventsForDay.Invoke(source.SaturdayChecked, DayOfWeek.Saturday);
            }

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

            public void AddScheduledStateInstruction(DateTime dateTime, bool turnOn, string timeZoneId = null)
            {
                RunStateInstructions.Add(GetUtcTimeOfWeekForAnyDateTime(dateTime, timeZoneId), turnOn);
            }

            // A convenience property to guarantee the consumer a collection that is ordered by increasing time. 
            [JsonIgnore]
            private List<KeyValuePair<TimeSpan, bool>> OrderedStateInstructionList => RunStateInstructions.OrderBy(i => i.Key).ToList();

            private TimeSpan GetUtcTimeOfWeekForAnyDateTime(DateTime dateTime, string timeZoneId = null)
            {
                if (timeZoneId != null)
                {
                    try
                    {
                        TimeZoneInfo tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                        dateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, tzInfo);
                    }
                    catch { }
                }

                dateTime = dateTime.ToUniversalTime();
                DateTime weekStart = dateTime.Date.AddDays(-(int)dateTime.DayOfWeek);
                return dateTime - weekStart;
            }
        }
        #endregion
    }
}

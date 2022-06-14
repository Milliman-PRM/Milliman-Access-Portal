/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A family of model classes representing root content item properties that are particular to the associated content type
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using MapDbContextLib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
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

    public enum ContainerCpuCoresEnum
    {
        [Display(Name = "Unspecified")]
        Unspecified = 0,
        [Display(Name = "1")]
        One = 1,
        [Display(Name = "2")]
        Two = 2,
        [Display(Name = "3")]
        Three = 3,
        [Display(Name = "4")]
        Four = 4,
    }

    public enum ContainerRamGbEnum
    {
        [Display(Name = "Unspecified")]
        Unspecified = 0,
        [Display(Name = "1")]
        One = 1,
        [Display(Name = "2")]
        Two = 2,
        [Display(Name = "3")]
        Three = 3,
        [Display(Name = "4")]
        Four = 4,
        [Display(Name = "5")]
        Five = 5,
        [Display(Name = "6")]
        Six = 6,
        [Display(Name = "7")]
        Seven = 7,
        [Display(Name = "8")]
        Eight = 8,
        [Display(Name = "9")]
        Nine = 9,
        [Display(Name = "10")]
        Ten = 10,
        [Display(Name = "11")]
        Eleven = 11,
        [Display(Name = "12")]
        Twelve = 12,
        [Display(Name = "13")]
        Thirteen = 13,
        [Display(Name = "14")]
        Fourteen = 14,
        [Display(Name = "15")]
        Fifteen = 15,
        [Display(Name = "16")]
        Sixteen = 16,
    }

    public enum ContainerCooldownPeriodEnum
    {
        [Display(Name = "Unspecified")]
        Unspecified = 0,
        [Display(Name = "30 minutes")]
        ThirtyMinutes = 1,
        [Display(Name = "1 hour")]
        OneHour = 2,
        [Display(Name = "90 minutes")]
        NinetyMinutes = 3,
        [Display(Name = "2 hours")]
        TwoHours = 4,
    }

    public enum ContainerInstanceLifetimeSchemeEnum
    {
        [Display(Name = "Unspecified")]
        Unspecified = 0,
        [Display(Name = "Always Cold")]
        AlwaysCold = 1,
        [Display(Name = "Custom")]
        Custom = 2,
    }

    public class ContainerizedAppRequestProperties
    {
        public ContainerCpuCoresEnum ContainerCpuCores { get; set; }

        public ContainerRamGbEnum ContainerRamGb { get; set; }

        public ushort ContainerInternalPort { get; set; }

        public ContainerCooldownPeriodEnum CustomCooldownPeriod { get; set; } = ContainerCooldownPeriodEnum.OneHour;

        public ContainerInstanceLifetimeSchemeEnum ContainerInstanceLifetimeScheme { get; set; } = ContainerInstanceLifetimeSchemeEnum.AlwaysCold;
        public bool? MondayChecked { get; set; }
        public bool? TuesdayChecked { get; set; }
        public bool? WednesdayChecked { get; set; }
        public bool? ThursdayChecked { get; set; }
        public bool? FridayChecked { get; set; }
        public bool? SaturdayChecked { get; set; }
        public bool? SundayChecked { get; set; }
        [JsonConverter(typeof(TimeSpanJsonConverter))]
        public TimeSpan? StartTime { get; set; }
        [JsonConverter(typeof(TimeSpanJsonConverter))]
        public TimeSpan? EndTime { get; set; }
        public string? TimeZoneId { get; set; }
    }

    internal class TimeSpanJsonConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            int selectedHour;
            try
            {
                reader.TryGetInt32(out selectedHour);
            }
            catch (InvalidOperationException)
            {
                selectedHour = Int32.Parse(reader.GetString());
            }
            return new TimeSpan(selectedHour, 0, 0);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Hours);
        }
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

            public LifetimeSchemeBase(ContainerizedAppRequestProperties source)
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
            public AlwaysColdLifetimeScheme(ContainerizedAppRequestProperties source) 
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

            public CustomScheduleLifetimeScheme(ContainerizedAppRequestProperties source)
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

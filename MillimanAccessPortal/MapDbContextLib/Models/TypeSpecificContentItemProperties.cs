/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A family of model classes representing root content item properties that are particular to the associated content type
 * DEVELOPER NOTES: <What future developers need to know.>
 */
using MapDbContextLib.Models;
using System;
using System.Collections.Generic;
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

    public class ContainerizedAppContentItemProperties : TypeSpecificContentItemProperties
    {
        public string LiveImageName { get; set; } = null;
        public string LiveImageTag { get; set; } = null;
        public ContainerCpuCoresEnum LiveContainerCpuCores { get; set; } = ContainerCpuCoresEnum.Unspecified;
        public ContainerRamGbEnum LiveContainerRamGb { get; set; } = ContainerRamGbEnum.Unspecified;
        public ushort LiveContainerInternalPort { get; set; } = 0;
        /// <summary>
        /// The dict key is the mount name, the dict value is the context string (future use)
        /// </summary>
        public Dictionary<string, string> LiveContainerStorageShares { get; set; } = new Dictionary<string, string>();

        [JsonConverter(typeof(LifetimeSchemeConverter))]
        public LifetimeSchemeBase LiveContainerLifetimeScheme { get; set; } = null;

        public string PreviewImageName { get; set; } = null;
        public string PreviewImageTag { get; set; } = null;
        public ContainerCpuCoresEnum PreviewContainerCpuCores { get; set; } = ContainerCpuCoresEnum.Unspecified;
        public ContainerRamGbEnum PreviewContainerRamGb { get; set; } = ContainerRamGbEnum.Unspecified;
        public ushort PreviewContainerInternalPort { get; set; } = 0;
        /// <summary>
        /// The dict key is the mount name, the dict value is the context string (future use)
        /// </summary>
        public Dictionary<string, string> PreviewContainerStorageShares { get; set; } = new Dictionary<string, string>();

        #region Lifetime management
        public abstract class LifetimeSchemeBase
        {
            public ContainerInstanceLifetimeSchemeEnum Scheme { get; set; } = ContainerInstanceLifetimeSchemeEnum.Unspecified;
            public TimeSpan ContainerLingerTimeAfterActivity { get; set; }

            public LifetimeSchemeBase()
            { }

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
            public AlwaysColdLifetimeScheme()
            {}

            public AlwaysColdLifetimeScheme(ContainerizedContentPublicationProperties source) 
                : base(source)
            {}
        }

        public bool DoesPublicationDetailChangeContentDetail(ContainerizedContentPublicationProperties requestProps)
        {
            bool returnValue = false;

            returnValue |= LiveContainerCpuCores != requestProps.ContainerCpuCores;
            returnValue |= LiveContainerRamGb != requestProps.ContainerRamGb;
            returnValue |= LiveContainerInternalPort != requestProps.ContainerInternalPort;
            returnValue |= LiveContainerLifetimeScheme.Scheme != requestProps.ContainerInstanceLifetimeScheme;
            returnValue |= !LiveContainerStorageShares.Any() && requestProps.DataPersistenceEnabled;

            if (LiveContainerLifetimeScheme.Scheme == ContainerInstanceLifetimeSchemeEnum.Custom && !returnValue)
            {
                CustomScheduleLifetimeScheme liveLifetimeScheme = (CustomScheduleLifetimeScheme)LiveContainerLifetimeScheme;
                CustomScheduleLifetimeScheme requestScheme = new CustomScheduleLifetimeScheme(requestProps);

                returnValue |= !requestScheme.Equals(liveLifetimeScheme);
            }

            return returnValue;
        }

        public class CustomScheduleLifetimeScheme : LifetimeSchemeBase, IEquatable<CustomScheduleLifetimeScheme>
        {
            /// <summary>
            /// The key must be relative to the start of the week (Saturday night midnight) in UTC, zero offset.
            /// Sequential ordering is not required
            /// </summary>
            public Dictionary<TimeSpan, bool> RunStateInstructions { get; } = new Dictionary<TimeSpan, bool>();

            public CustomScheduleLifetimeScheme()
            {}

            public bool Equals(CustomScheduleLifetimeScheme other)
            {
                return OrderedStateInstructionList.SequenceEqual(other.OrderedStateInstructionList);
            }

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

            public bool IsScheduledOnNow(TimeSpan? bufTime = null)
            {
                TimeSpan bufferTime = bufTime ?? TimeSpan.Zero;
                TimeSpan utcNowTimeOfWeek = GetUtcTimeOfWeekForAnyDateTime(DateTime.UtcNow);

                Func<KeyValuePair<TimeSpan, bool>, TimeSpan> GetEffectiveTime = kvp => kvp.Value
                                                                                       ? kvp.Key - bufferTime
                                                                                       : kvp.Key + bufferTime;

                switch (OrderedStateInstructionList)
                {
                    // nothing configured or no start times configured
                    case var x when !x.Any():
                    case var y when !y.Any(i => i.Value == true):
                        return false;

                    // utcNowTimeOfWeek is before the first configured event of the week
                    case var x when utcNowTimeOfWeek < GetEffectiveTime(x.First()):
                        return x.Last().Value;

                    // There is an applicable instruction in OrderedStateInstructionList
                    default:
                        KeyValuePair<TimeSpan, bool> mostRecentInstructionBeforeNow = OrderedStateInstructionList.Last(i => utcNowTimeOfWeek >= GetEffectiveTime(i));
                        return mostRecentInstructionBeforeNow.Value;
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

        public class LifetimeSchemeConverter : JsonConverter<LifetimeSchemeBase>
        {
            public override void Write(Utf8JsonWriter writer, LifetimeSchemeBase value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WriteNumber("scheme", (int)value.Scheme);

                switch (value.GetType())
                {
                    case var x when x == typeof(AlwaysColdLifetimeScheme):
                        break;

                    case var x when x == typeof(CustomScheduleLifetimeScheme):
                        CustomScheduleLifetimeScheme customScheme = value as CustomScheduleLifetimeScheme;
                        writer.WriteStartObject("RunStateInstructions");
                        foreach (var instruction in customScheme.RunStateInstructions)
                        {
                            writer.WriteBoolean(instruction.Key.ToString(), instruction.Value);
                        }
                        writer.WriteEndObject();
                        break;

                    default:
                        throw new NotImplementedException("This json converter cannot serialize object of unsupported type " + value.GetType().Name);
                }

                writer.WriteString("ContainerLingerTimeAfterActivity", value.ContainerLingerTimeAfterActivity.ToString());

                writer.WriteEndObject();
            }

            public override LifetimeSchemeBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Dictionary<string, object> allProperties = new Dictionary<string, object>();
                string currentPropertyName = string.Empty;

                bool continueReading = true;
                int objectDepth = 0;
                while (continueReading && reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            currentPropertyName = reader.GetString().ToLower();
                            break;

                        case JsonTokenType.True:
                        case JsonTokenType.False:
                            bool boolValue = reader.GetBoolean();
                            allProperties[currentPropertyName] = boolValue;
                            break;

                        case JsonTokenType.String:
                            {
                                string? text = reader.GetString();
                                allProperties[currentPropertyName] = text;
                                break;
                            }

                        case JsonTokenType.Number:
                            {
                                int intValue = reader.GetInt32();
                                allProperties[currentPropertyName] = intValue;
                                break;
                            }

                        case JsonTokenType.StartObject:
                            objectDepth++;
                            break;

                        case JsonTokenType.EndObject:
                            if (objectDepth == 0)
                            {
                                continueReading = false;
                            }
                            else
                            {
                                objectDepth--;
                            }
                            break;

                        default:
                            string otherString = reader.GetString();
                            break;
                    }
                }

                if (! allProperties.TryGetValue("scheme", out object schemeValue))
                {
                    throw new ApplicationException("Failed to deserialize container type specific content item properties.  \"Scheme\" property not found");
                }
                ContainerInstanceLifetimeSchemeEnum schemeEnum = (ContainerInstanceLifetimeSchemeEnum)schemeValue;

                switch (schemeEnum)
                {
                    case ContainerInstanceLifetimeSchemeEnum.AlwaysCold:
                        return new AlwaysColdLifetimeScheme
                        {
                            Scheme = schemeEnum,
                            ContainerLingerTimeAfterActivity = TimeSpan.Parse(allProperties["containerlingertimeafteractivity"] as string)
                        };

                    case ContainerInstanceLifetimeSchemeEnum.Custom:
                        CustomScheduleLifetimeScheme retVal = new CustomScheduleLifetimeScheme
                        {
                            Scheme = schemeEnum,
                            ContainerLingerTimeAfterActivity = TimeSpan.Parse(allProperties["containerlingertimeafteractivity"] as string),
                        };
                        foreach (KeyValuePair<string, object> kvp in allProperties)
                        {
                            if (TimeSpan.TryParse(kvp.Key, out TimeSpan ts) &&
                                kvp.Value.GetType() == typeof(bool))
                            {
                                retVal.RunStateInstructions.Add(ts, (bool)kvp.Value);
                            }
                        }

                        return retVal;
                }
                return null;
            }

            LifetimeSchemeBase startValue = default(LifetimeSchemeBase);

        }
        #endregion
    }
}

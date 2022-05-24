/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines the structure of publication request properties that depend on the content type of the content item
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MapDbContextLib.Models
{
    public class TypeSpecificPublicationPropertiesBase
    {}

    public class PowerBiPublicationProperties : TypeSpecificPublicationPropertiesBase
    {
        public List<string> RoleList { get; set; } = null;
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

    public class ContainerizedContentPublicationProperties : TypeSpecificPublicationPropertiesBase
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
            catch (InvalidOperationException ex)
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
}

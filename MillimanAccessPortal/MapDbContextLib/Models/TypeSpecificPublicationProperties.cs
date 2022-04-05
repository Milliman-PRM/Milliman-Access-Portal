/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines the structure of publication request properties that depend on the content type of the content item
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

    public class ContainerizedContentPublicationProperties : TypeSpecificPublicationPropertiesBase
    {
        public ContainerCpuCoresEnum ContainerCpuCores { get; set; }

        public ContainerRamGbEnum ContainerRamGb { get; set; }

        public ushort ContainerInternalPort { get; set; }
    }
}

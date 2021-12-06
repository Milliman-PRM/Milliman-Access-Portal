/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines the structure of publication request properties that depend on the content type of the content item
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;

namespace MapDbContextLib.Models
{
    public class TypeSpecificPublicationPropertiesBase
    {}

    public class PowerBiPublicationProperties : TypeSpecificPublicationPropertiesBase
    {
        public List<string> RoleList { get; set; } = null;
    }

    public class ContainerizedContentPublicationProperties : TypeSpecificPublicationPropertiesBase
    {
        public enum ContainerCpuCoresEnum
        {
            Unspecified = 0,
            One = 1,
            Two = 2,
            Three = 3,
            Four = 4,
        }

        public enum ContainerRamGbEnum
        {
            Unspecified = 0,
            One = 1,
            Two = 2,
            Three = 3,
            Four = 4,
            Five = 5,
            Six = 6,
            Seven = 7,
            Eight = 8,
            Nine = 9,
            Ten = 10,
            Eleven = 11,
            Twelve = 12,
            Thirteen = 13,
            Fourteen = 14,
            Fifteen = 15,
            Sixteen = 16,
        }

        public ContainerCpuCoresEnum ContainerCpuCores { get; set; }

        public ContainerRamGbEnum ContainerRamGb { get; set; }

        public uint ContainerInternalPort { get; set; }
    }
}

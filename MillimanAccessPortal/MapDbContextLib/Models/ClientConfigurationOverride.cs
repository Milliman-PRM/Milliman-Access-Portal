/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Properties used to override configuration elements that relate to an instance of Client
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;

namespace MapDbContextLib.Models
{
    public class ClientConfigurationOverride
    {
        public Guid? PowerBiCapacityId { get; set; } = null;
    }
}

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
}

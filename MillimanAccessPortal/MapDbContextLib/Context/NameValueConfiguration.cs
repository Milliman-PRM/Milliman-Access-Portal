/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.ComponentModel.DataAnnotations;

namespace MapDbContextLib.Context
{
    public class NameValueConfiguration
    {
        [Key]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }
    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a root content item in the persistence data model
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class ContentType
    {
        public long Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public bool CanReduce { get; set; }

    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a root content item in the persistence data model
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace MapDbContextLib.Context
{
    public class RootContentItem
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string ContentName { get; set; }

        [ForeignKey("ContentType")]
        public long ContentTypeId { get; set; }
        public ContentType ContentType { get; set; }

        [ForeignKey("Client")]
        public long ClientId { get; set; }
        public Client Client { get; set; }

        [Required]
        public bool DoesReduce { get; set; }

        [Column(TypeName ="jsonb")]
        // [Required] This causes a problem with migration database update
        public string TypeSpecificDetail { get; set; }
    }
}

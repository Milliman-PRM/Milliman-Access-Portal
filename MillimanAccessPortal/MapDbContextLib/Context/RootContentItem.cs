/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a root content item in the persistence data model
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class RootContentItem
    {
        public long Id { get; set; }

        public string ContentType { get; set; }

        [ForeignKey("OwningClient")]
        public long ClientId { get; set; }
        public Client OwningClient { get; set; }

        public string ContentName { get; set; }

    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a root content item in the persistence data model
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace MapDbContextLib.Context
{
    public class RootContentItem
    {
        public long Id { get; set; }

        public string ContentName { get; set; }

        [ForeignKey("ContentType")]
        public long ContentTypeId { get; set; }
        public ContentType ContentType { get; set; }

        public List<long> ClientIdList { get; set; }

    }
}

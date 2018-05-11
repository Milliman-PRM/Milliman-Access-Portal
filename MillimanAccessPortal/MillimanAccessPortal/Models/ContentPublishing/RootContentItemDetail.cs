/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: Model root content item details available to users
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using System.Collections.Generic;
using System.Linq;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemDetail
    {
        public long Id { get; set; }
        public long ClientId { get; set; }
        public string ContentName { get; set; }
        public ContentType ContentType { get; set; }
        public List<ContentType> AvailableContentTypes { get; set; }
        public bool DoesReduce { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }

        internal static RootContentItemDetail Build(ApplicationDbContext dbContext ,RootContentItem rootContentItem)
        {
            RootContentItemDetail model = new RootContentItemDetail
            {
                Id = rootContentItem.Id,
                ClientId = rootContentItem.ClientId,
                ContentName = rootContentItem.ContentName,
                ContentType = dbContext.ContentType.Find(rootContentItem.ContentTypeId),
                AvailableContentTypes = dbContext.ContentType.ToList(),
                DoesReduce = true,
                Description = "Sample description",
                Notes = "Sample notes",
            };

            return model;
        }
    }
}

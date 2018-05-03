/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: Model root content item details available to users
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemDetail
    {
        public long Id { get; set; }
        public string ContentName { get; set; }
        public ContentType ContentType { get; set; }
        public bool DoesReduce { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }

        internal static RootContentItemDetail Build(ApplicationDbContext dbContext ,RootContentItem rootContentItem)
        {
            RootContentItemDetail model = new RootContentItemDetail
            {
                Id = rootContentItem.Id,
                ContentName = rootContentItem.ContentName,
                ContentType = dbContext.ContentType.Find(rootContentItem.ContentTypeId),
                DoesReduce = true,
                Description = "Sample description",
                Notes = "Sample notes",
            };

            return model;
        }
    }
}

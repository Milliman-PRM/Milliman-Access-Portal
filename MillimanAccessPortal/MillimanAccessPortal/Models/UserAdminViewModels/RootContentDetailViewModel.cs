/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A ViewModel representing details of a RootContentItem for use in UserAdmin
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.UserAdminViewModels
{
    public class RootContentDetailViewModel
    {
        public string ContentName { get; set; }
        public string ContentType { get; set; }
        public bool CanReduce { get; set; }
        public int NumberOfGroups { get; set; }
        public int NumberOfAssignedUsers { get; set; }

        /// <summary>
        /// Converts a RootContentItem to an instance of this ViewModel type
        /// </summary>
        /// <param name="Arg">A valid instance of RootContentItem from the database</param>
        /// <param name="DbContext">This must ba a valid ApplicationDbContext</param>
        /// <returns></returns>
        internal static RootContentDetailViewModel GetModel(RootContentItem Arg, ApplicationDbContext DbContext=null)
        {
            if (Arg.ContentType == null)
            {
                if (DbContext != null)
                {
                    Arg.ContentType = DbContext.ContentType.Find(Arg.ContentTypeId);
                }
                else
                {
                    throw new MapCommonLib.MapException("ContentType not provided while building RootContentDetailViewModel");
                }
            }

            // Read supporting data from the database
            List<UserInContentItemUserGroup> ReferencingUsersAndGroups = DbContext.UserInContentItemUserGroup
                                                                                  .Include(ug => ug.ContentItemUserGroup)
                                                                                  .Where(u => u.ContentItemUserGroup.RootContentItemId == Arg.Id)
                                                                                  .ToList();

            // Instantiate/initialize the model to be returend
            RootContentDetailViewModel ReturnModel = new RootContentDetailViewModel { ContentName = Arg.ContentName,
                                                                                      ContentType = Arg.ContentType.Name,
                                                                                      CanReduce = Arg.ContentType.CanReduce,
                                                                                      NumberOfGroups = ReferencingUsersAndGroups.Select(u => u.ContentItemUserGroupId).Distinct().Count(),
                                                                                      NumberOfAssignedUsers = ReferencingUsersAndGroups.Select(u => u.UserId).Distinct().Count() };

            return ReturnModel;
        }

        /// <summary>
        /// Returns an instance of this ViewModel type representing the RootContentItem identified by RootContentId
        /// </summary>
        /// <param name="RootContentId">The primary key value of a RootContentItem record in the database</param>
        /// <param name="DbContext">This must ba a valid ApplicationDbContext</param>
        /// <returns></returns>
        internal static RootContentDetailViewModel GetModel(long RootContentId, ApplicationDbContext DbContext)
        {
            RootContentItem Content = DbContext.RootContentItem
                                               .Include(rc => rc.ContentType)
                                               .Single(rc => rc.Id == RootContentId);

            return GetModel(Content, DbContext);
        }
    }
}

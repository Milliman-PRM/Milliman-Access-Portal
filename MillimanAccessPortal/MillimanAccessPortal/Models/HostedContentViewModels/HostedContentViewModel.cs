using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.HostedContentViewModels
{
    /// <summary>
    /// A POCO class representing each authorized content item to be presented in the content index page
    /// Anything that gets altered about this class must be reflected in StandardQueries.GetAuthorizedUserGroupsAndRoles() and other places
    /// </summary>
    public class HostedContentViewModel
    {
        public class ParentClientTree
        {
            public long Id;
            public long? ParentId;
            public string Name;
        }

        public long UserGroupId { get; set; }

        public string Url { get; set; }

        public string ContentName { get; set; }

        public HashSet<string> RoleNames { get; set; }

        /// <summary>
        /// First element should be the root level Client, list order traces down to the Client directly referenced by the ContentItemUserGroup
        /// </summary>
        public List<ParentClientTree> ClientList { get; set; }
    }
}

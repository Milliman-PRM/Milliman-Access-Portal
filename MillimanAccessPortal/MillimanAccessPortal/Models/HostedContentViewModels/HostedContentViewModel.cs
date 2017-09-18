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
        public long UserGroupId { get; set; }
        public string Url { get; set; }
        public string ContentName { get; set; }
        public HashSet<string> RoleNames { get; set; }
    }
}

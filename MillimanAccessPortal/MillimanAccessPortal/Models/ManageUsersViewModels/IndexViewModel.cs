using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ManageUsersViewModels
{
    public class IndexViewModel
    {
        IQueryable<ApplicationUser> Users { get; } 
    }
}

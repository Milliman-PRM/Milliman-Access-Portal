using MapCommonLib.ActionFilters;
using MillimanAccessPortal.Models.AccountViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
  public class SaveNewClientUserResponseModel
  {
    /// <summary>
    /// UserInfoViewModel of the new Client User.
    /// </summary>
    [EmitBeforeAfterLog]
    public UserInfoViewModel UserInfo { get; set; }

    /// <summary>
    /// Role assignments given to the new Client User.
    /// </summary>
    [EmitBeforeAfterLog]
    public Dictionary<int, ClientRoleAssignment> UserRoles { get; set; }
  }
}

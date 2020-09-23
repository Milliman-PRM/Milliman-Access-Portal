using MapCommonLib.ActionFilters;
using MapDbContextLib.Context;
using MillimanAccessPortal.Models.ContentAccessAdmin;
using MillimanAccessPortal.Models.EntityModels.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
  public class SaveNewClientResponseModel
  {
    /// <summary>
    /// The newly created client.
    /// </summary>
    [EmitBeforeAfterLog]
    public ClientDetail NewClient { get; set; }

    /// <summary>
    /// ClientsResponseModel containing dicts of clients which a client-admin has access to.
    /// </summary>
    [EmitBeforeAfterLog]
    public Dictionary<Guid, BasicClientWithEligibleUsers> Clients { get; set; } = 
      new Dictionary<Guid, BasicClientWithEligibleUsers>();

  }
}

/*
 * CODE OWNERS: Evan Klein
 * OBJECTIVE: Response model for delivering a list of all active clients for an authorized user, as well as
 * the client details for the newly created client.
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib.ActionFilters;
using MillimanAccessPortal.Models.EntityModels.ClientModels;
using System;
using System.Collections.Generic;

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

    /// <summary>
    /// The new client-admin for the newly created client.
    /// </summary>
    [EmitBeforeAfterLog]
    public UserInfoModel AssignedUser { get; set; } = new UserInfoModel();
  }
}

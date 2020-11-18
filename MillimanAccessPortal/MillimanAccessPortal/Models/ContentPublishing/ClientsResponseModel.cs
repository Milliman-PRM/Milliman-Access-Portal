/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MillimanAccessPortal.Models.EntityModels.ClientModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class ClientsResponseModel
    {
        public Dictionary<Guid, BasicClientWithCardStats> Clients { get; set; }
    }
}

/*
 * CODE OWNERS: Mike Reisz, Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MillimanAccessPortal.Models.EntityModels.ClientModels;
using MillimanAccessPortal.Models.UserModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class ClientsModel
    {
        public Dictionary<Guid, ClientCardModel> Clients { get; set; }
    }
}

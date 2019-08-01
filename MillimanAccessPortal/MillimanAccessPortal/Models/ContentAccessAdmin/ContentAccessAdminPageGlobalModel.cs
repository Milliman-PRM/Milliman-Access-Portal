/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class ContentAccessAdminPageGlobalModel
    {
        public Dictionary<Guid, BasicContentType> ContentTypes { get; set; }
    }
}

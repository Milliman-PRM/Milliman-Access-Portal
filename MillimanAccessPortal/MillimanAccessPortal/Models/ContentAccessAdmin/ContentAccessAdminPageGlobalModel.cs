/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines information used globally by the ContentAccessAdmin View
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using System;
using System.Collections.Generic;

namespace MillimanAccessPortal.Models.ContentAccessAdmin
{
    public class ContentAccessAdminPageGlobalModel
    {
        public Dictionary<Guid, BasicContentType> ContentTypes { get; set; }
    }
}

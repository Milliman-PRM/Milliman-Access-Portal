/*
 * CODE OWNERS: Joseph Sweeney,
 * OBJECTIVE: Provide root content item information for display in the system admin detail panel
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using System;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class RootContentItemDetailForUser
    {
        public Guid Id { get; set; }
        public string ContentName { get; set; }
        public string ContentType { get; set; }

        public static explicit operator RootContentItemDetailForUser(RootContentItem contentItem)
        {
            if (contentItem == null)
            {
                return null;
            }

            return new RootContentItemDetailForUser
            {
                Id = contentItem.Id,
                ContentName = contentItem.ContentName,
                ContentType = (contentItem.ContentType?.TypeEnum ?? ContentTypeEnum.Unknown).GetDisplayNameString(),
            };
        }
    }
}

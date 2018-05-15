/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.AccountViewModels;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class PublicationDetails
    {
        public UserInfoViewModel User { get; set; }
        public PublicationStatus StatusEnum { get; set; }
        public string StatusName { get => ContentPublicationRequest.PublicationStatusString[StatusEnum]; }
        public long RootContentItemId { get; set; }

        public static explicit operator PublicationDetails(ContentPublicationRequest contentPublicationRequest)
        {
            if (contentPublicationRequest == null)
            {
                return null;
            }
            return new PublicationDetails
            {
                User = (UserInfoViewModel) contentPublicationRequest.ApplicationUser,
                StatusEnum = contentPublicationRequest.RequestStatus,
                RootContentItemId = contentPublicationRequest.RootContentItemId,
            };
        }
    }
}

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.UserModels;
using System;

namespace MillimanAccessPortal.Models.EntityModels.PublicationModels
{
    /// <summary>
    /// A simplified representation of a ContentPublicationRequest.
    /// This model is intended to be extended to satisfy front end needs.
    /// </summary>
    public class BasicPublication
    {
        public Guid Id { get; set; }
        public Guid RootContentItemId { get; set; }
        public BasicUser ApplicationUser { get; set; }
        public DateTime CreateDateTimeUtc { get; set; }
        public PublicationStatus RequestStatus { get; set; }

        /// <summary>
        /// Type conversion from ContentPublicationRequest to BasicPublication
        /// </summary>
        /// <param name="publication">The ApplicationUser navigation property should be populated</param>
        public static explicit operator BasicPublication(ContentPublicationRequest publication)
        {
            if (publication == null)
            {
                return null;
            }

            return new BasicPublication
            {
                Id = publication.Id,
                RootContentItemId = publication.RootContentItemId,
                ApplicationUser = (BasicUser)publication.ApplicationUser,
                CreateDateTimeUtc = publication.CreateDateTimeUtc,
                RequestStatus = publication.RequestStatus,
            };
        }
    }
}

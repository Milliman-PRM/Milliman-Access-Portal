using MapDbContextLib.Context;
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
        public Guid ApplicationUserId { get; set; }
        public DateTime CreateDateTimeUtc { get; set; }
        public PublicationStatus RequestStatus { get; set; }

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
                ApplicationUserId = publication.ApplicationUserId,
                CreateDateTimeUtc = publication.CreateDateTimeUtc,
                RequestStatus = publication.RequestStatus,
            };
        }
    }
}

/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System.ComponentModel.DataAnnotations;

namespace MapDbContextLib.Context
{
    public class ContentPublicationRequestStatus
    {
        [Key]
        public long ContentPublicationRequestId { get; set; }

        public ReductionStatusEnum PublicationRequestStatus { get; set; }
    }
}

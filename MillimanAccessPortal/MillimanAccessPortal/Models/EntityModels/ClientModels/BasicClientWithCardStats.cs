/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ClientModels
{
    public class BasicClientWithCardStats : BasicClient
    {
        public BasicClientWithCardStats() { }
        public BasicClientWithCardStats(Client b) : base (b) {}

        /// <summary>
        /// Number of RootContentItems for this client.
        /// </summary>
        public int ContentItemCount { get; set; }

        /// <summary>
        /// Number of users in this client.
        /// This count is greater than or equal to the number of content eligible users in the client.
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// Indication of a user's authorization to administer this client based on a specified client role
        /// </summary>
        public bool CanManage { get; set; }
    }
}

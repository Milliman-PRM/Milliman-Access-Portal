/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Extends the base class with properties employed in the user interface
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.EntityModels.ClientModels
{
    public class BasicClientWithCardStats : BasicClient
    {
        public BasicClientWithCardStats() { }

        /// <summary>
        /// Populates only the fields of (base) class BasicClient.  Other property values may depend on context of the caller so should be assigned there. 
        /// </summary>
        /// <param name="c"></param>
        public BasicClientWithCardStats(Client c) : base(c) { }

        public static explicit operator BasicClientWithCardStats(Client c)
        {
            return new BasicClientWithCardStats(c);
        }

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

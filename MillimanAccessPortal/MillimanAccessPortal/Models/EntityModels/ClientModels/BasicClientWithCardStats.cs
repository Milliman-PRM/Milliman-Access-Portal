namespace MillimanAccessPortal.Models.ClientModels
{
    public class BasicClientWithCardStats : BasicClient
    {
        /// <summary>
        /// Number of RootContentItems for this client.
        /// </summary>
        public int ContentItemCount { get; set; }

        /// <summary>
        /// Number of users in this client.
        /// This count is greater than or equal to the number of content eligible users in the client.
        /// </summary>
        public int UserCount { get; set; }
    }
}

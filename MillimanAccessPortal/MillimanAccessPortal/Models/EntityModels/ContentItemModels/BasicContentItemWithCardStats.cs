namespace MillimanAccessPortal.Models.EntityModels.ContentItemModels
{
    public class BasicContentItemWithCardStats : BasicContentItem
    {
        /// <summary>
        /// The number of selection groups for this RootContentItem
        /// </summary>
        public int SelectionGroupCount { get; set; }

        /// <summary>
        /// The number of users assigned to selection groups for this AssignedUserCount
        /// </summary>
        public int AssignedUserCount { get; set; }
    }
}

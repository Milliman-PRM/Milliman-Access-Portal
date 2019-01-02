namespace MillimanAccessPortal.Models.EntityModels.ContentItemModels
{
    public class BasicContentItemWithStats : BasicContentItem
    {
        public int SelectionGroupCount { get; set; }
        public int AssignedUserCount { get; set; }
    }
}

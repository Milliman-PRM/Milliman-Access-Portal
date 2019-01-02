namespace MillimanAccessPortal.Models.ClientModels
{
    public class BasicClientWithStats : BasicClient
    {
        public int ContentItemCount { get; set; }
        public int UserCount { get; set; }
    }
}

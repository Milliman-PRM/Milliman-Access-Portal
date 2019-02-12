namespace MillimanAccessPortal.Models.ClientModels
{
    public class BasicClientWithCardStats : BasicClient
    {
        public int ContentItemCount { get; set; }
        public int UserCount { get; set; }
    }
}

using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.AuthorizedContentViewModels
{
    public class ContentWrapperModel
    {
        public string ContentURL { get; set; }
        public ContentTypeEnum ContentType { get; set; }
    }
}

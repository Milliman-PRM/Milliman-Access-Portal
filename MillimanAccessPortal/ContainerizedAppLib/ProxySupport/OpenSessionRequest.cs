namespace ContainerizedAppLib.ProxySupport
{
    public class OpenSessionRequest
    {
        public string RequestingHost { get; set; }
        public string Token { get; set; }
        public string PublicUri { get; set; }
        public string InternalUri { get; set; }
    }
}

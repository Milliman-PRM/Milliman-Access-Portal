namespace MapDbContextLib.Models
{
    public enum RequestErrorReason
    {
        Default = 0,
        ReductionTaskError = 1,
    }

    public class RequestMetadata
    {
        public RequestErrorReason ErrorReason { get; set; } = RequestErrorReason.Default;
    }
}

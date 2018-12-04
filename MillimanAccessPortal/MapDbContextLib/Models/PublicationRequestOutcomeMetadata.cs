namespace MapDbContextLib.Models
{
    public enum PublicationRequestErrorReason
    {
        Default = 0,
        ReductionTaskError = 1,
    }

    public class PublicationRequestOutcomeMetadata
    {
        public PublicationRequestErrorReason ErrorReason { get; set; } = PublicationRequestErrorReason.Default;
    }
}

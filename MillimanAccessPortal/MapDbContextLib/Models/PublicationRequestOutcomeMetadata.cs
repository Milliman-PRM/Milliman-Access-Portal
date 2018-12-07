/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE: Declares the class representing overall outcome of publication request processing
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System.Collections.Generic;

namespace MapDbContextLib.Models
{
    public enum PublicationRequestErrorReason
    {
        Default = 0,
        ReductionTaskError = 1,
    }

    public class PublicationRequestOutcomeMetadata
    {
        public List<ReductionTaskOutcomeMetadata> ReductionTaskFailOutcomeList = new List<ReductionTaskOutcomeMetadata>();
        public List<ReductionTaskOutcomeMetadata> ReductionTaskSuccessOutcomeList = new List<ReductionTaskOutcomeMetadata>();
    }
}

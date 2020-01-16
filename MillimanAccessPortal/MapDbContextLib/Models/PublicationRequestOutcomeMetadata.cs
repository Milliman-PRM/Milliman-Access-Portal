/*
 * CODE OWNERS: Joseph Sweeney, Tom Puckett
 * OBJECTIVE: Declares the class representing overall outcome of publication request processing
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;

namespace MapDbContextLib.Models
{
    public class PublicationRequestOutcomeMetadata
    {
        public Guid Id { get; set; }
        public DateTime StartDateTime { get; set; }
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
        public string UserMessage { get; set; } = default;
        public string SupportMessage { get; set; } = default;
        public List<ReductionTaskOutcomeMetadata> ReductionTaskFailOutcomeList { get; set; } = new List<ReductionTaskOutcomeMetadata>();
        public List<ReductionTaskOutcomeMetadata> ReductionTaskSuccessOutcomeList { get; set; } = new List<ReductionTaskOutcomeMetadata>();
    }
}

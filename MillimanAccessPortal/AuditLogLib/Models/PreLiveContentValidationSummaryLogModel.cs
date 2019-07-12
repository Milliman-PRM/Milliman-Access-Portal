/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using MapDbContextLib.Models;

namespace AuditLogLib.Models
{
    public class PreLiveContentValidationSummaryLogModel
    {
        public string ValidationSummaryId;
        public Guid PublicationRequestId;
        public string AttestationLanguage;
        public string ContentDescription;
        public Guid RootContentId;
        public string RootContentName;
        public string ContentTypeName;
        public ContentReductionHierarchy<ReductionFieldValue> LiveHierarchy;
        public ContentReductionHierarchy<ReductionFieldValue> NewHierarchy;
        public bool  DoesReduce;
        public Guid ClientId;
        public string ClientName;
    }
}

/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using Newtonsoft.Json.Linq;
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
        public ContentReductionHierarchy<ReductionFieldValueChange> HierarchyComparison;
        public bool  DoesReduce;
        public Guid ClientId;
        public string ClientName;
        public string ClientCode;
        public JArray SelectionGroupSummary;
    }
}

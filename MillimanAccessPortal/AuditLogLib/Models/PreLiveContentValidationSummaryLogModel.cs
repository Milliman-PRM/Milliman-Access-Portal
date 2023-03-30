/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

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
        public JsonArray SelectionGroupSummary;
        public Dictionary<string, object> TypeSpecificMetadata;
    }
}

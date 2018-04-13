/*
 * CODE OWNERS: Tom Puckett, Joseph Sweeney
 * OBJECTIVE: Represents reduction job outputs and status, agnostic to the types used by the application that originated the queued task
 * DEVELOPER NOTES: This gets converted to queue specific 
 */

using System;
using System.Linq;
using System.Collections.Generic;
using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Newtonsoft.Json;

namespace ContentReductionLib.ReductionRunners
{
    internal enum JobStatusEnum
    {
        Unspecified,
        Canceled,
        Success,
        Error,
    }

    public enum JobActionEnum
    {
        Unspecified = 0,    // Default unknown state
        HierarchyOnly = 1,
        HierarchyAndReduction = 2,
    }

    /// <summary>
    /// Internal representation of the job request and result
    /// Each queue type typically submits selections in its own typed fashion so a cast operator will be needed for each new source
    /// </summary>
    internal class ReductionJobDetail
    {
        internal ReductionJobRequest Request;
        internal ReductionJobResult Result;
        internal Guid TaskId { get; set; } = Guid.Empty;

        // cast operator to convert a MAP ContentReductionTask to this type
        public static explicit operator ReductionJobDetail(ContentReductionTask T)
        {
            ContentReductionHierarchy<ReductionFieldValueSelection> MapSelections = T.SelectionCriteria != null
                ? JsonConvert.DeserializeObject<ContentReductionHierarchy<ReductionFieldValueSelection>>(T.SelectionCriteria)
                : new ContentReductionHierarchy<ReductionFieldValueSelection>();

            return new ReductionJobDetail
            {
                TaskId = T.Id,
                Request = new ReductionJobRequest
                {
                    MasterFilePath = T.MasterFilePath,
                    SelectionCriteria = MapSelections.Fields
                                                     .SelectMany(f => f.Values
                                                                       .Select(v => new FieldValueSelection { FieldName = f.FieldName, FieldValue = v.Value, Selected = v.SelectionStatus }))
                                                     .ToList(),
                    MasterContentChecksum = T.MasterContentChecksum,
                    JobAction = T.TaskAction == TaskActionEnum.HierarchyOnly 
                                ? JobActionEnum.HierarchyOnly
                                : T.TaskAction == TaskActionEnum.HierarchyAndReduction 
                                ? JobActionEnum.HierarchyAndReduction
                                : JobActionEnum.Unspecified,
                },
                Result = new ReductionJobResult(),
            };

        }

        internal class ReductionJobResult
        {
            internal JobStatusEnum Status { get; set; } = JobStatusEnum.Unspecified;
            internal string StatusMessage { get; set; } = string.Empty;
            internal string ReducedContentFilePath { get; set; } = string.Empty;
            internal ExtractedHierarchy MasterContentHierarchy { get; set; } = null;
            internal ExtractedHierarchy ReducedContentHierarchy { get; set; } = null;
            internal string ReducedContentFileChecksum { get; set; } = string.Empty;
        }

        internal class ReductionJobRequest
        {
            internal string MasterFilePath { get; set; }
            internal List<FieldValueSelection> SelectionCriteria { get; set; }
            internal string MasterContentChecksum { get; set; } = string.Empty;
            internal JobActionEnum JobAction { get; set; } = JobActionEnum.Unspecified;
        }
    }
}

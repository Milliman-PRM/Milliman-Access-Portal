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

namespace ContentPublishingLib.ReductionRunners
{
    public enum JobStatusEnum
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
    public class ReductionJobDetail
    {
        public ReductionJobRequest Request;
        public ReductionJobResult Result;
        public Guid TaskId { get; set; } = Guid.Empty;

        // cast operator to convert a MAP ContentReductionTask to this type
        public static explicit operator ReductionJobDetail(ContentReductionTask DbTask)
        {
            ContentReductionHierarchy<ReductionFieldValueSelection> MapSelections = DbTask.SelectionCriteria != null
                ? JsonConvert.DeserializeObject<ContentReductionHierarchy<ReductionFieldValueSelection>>(DbTask.SelectionCriteria)
                : new ContentReductionHierarchy<ReductionFieldValueSelection>();

            return new ReductionJobDetail
            {
                TaskId = DbTask.Id,
                Request = new ReductionJobRequest
                {
                    MasterFilePath = DbTask.MasterFilePath,
                    SelectionCriteria = MapSelections.Fields
                                                     .SelectMany(f => f.Values
                                                                       .Select(v => new FieldValueSelection { FieldName = f.FieldName, FieldValue = v.Value, Selected = v.SelectionStatus }))
                                                     .ToList(),
                    MasterContentChecksum = DbTask.MasterContentChecksum,
                    JobAction = DbTask.TaskAction == TaskActionEnum.HierarchyOnly 
                                ? JobActionEnum.HierarchyOnly
                                : DbTask.TaskAction == TaskActionEnum.HierarchyAndReduction 
                                ? JobActionEnum.HierarchyAndReduction
                                : JobActionEnum.Unspecified,
                },
                Result = new ReductionJobResult(),
            };

        }

        public class ReductionJobResult
        {
            public JobStatusEnum Status { get; set; } = JobStatusEnum.Unspecified;
            public string StatusMessage { get; set; } = string.Empty;
            public string ReducedContentFilePath { get; set; } = string.Empty;
            public ExtractedHierarchy MasterContentHierarchy { get; set; } = null;
            public ExtractedHierarchy ReducedContentHierarchy { get; set; } = null;
            public string ReducedContentFileChecksum { get; set; } = string.Empty;
        }

        public class ReductionJobRequest
        {
            public string MasterFilePath { get; set; }
            public List<FieldValueSelection> SelectionCriteria { get; set; }
            public string MasterContentChecksum { get; set; } = string.Empty;
            public JobActionEnum JobAction { get; set; } = JobActionEnum.Unspecified;
        }
    }
}

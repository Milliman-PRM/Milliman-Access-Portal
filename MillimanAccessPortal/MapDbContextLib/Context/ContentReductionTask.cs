/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Represents a reduction server task
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Models;
using MapDbContextLib.Identity;

namespace MapDbContextLib.Context
{
    public class ContentReductionTask
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("ContentPublicationRequest")]
        public long ContentPublicationRequestId { get; set; }
        public ContentPublicationRequest ContentPublicationRequest { get; set; }

        [ForeignKey("SelectionGroup")]
        public long SelectionGroupId { get; set; }
        public SelectionGroup SelectionGroup { get; set; }

        [Required]
        public string Status { get; set; }

        /// <summary>
        /// This path must be accessible to MAP application and reduction server.  
        /// May be different from master file in ContentPublicationRequest
        /// </summary>
        [Required]
        public string MasterContentFile { get; set; }

        /// <summary>
        /// null if reduction not requested.  Path must be accessible to MAP application and reduction server
        /// </summary>
        public string ResultContentFile { get; set; }

        [Column(TypeName ="jsonb")]
        public string SelectionCriteria { get; set; }
    }
}


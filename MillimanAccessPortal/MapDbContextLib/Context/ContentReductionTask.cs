using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MapDbContextLib.Models;

namespace MapDbContextLib.Context
{
    public class ContentReductionTask
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        // Default value is enforced in ApplicationDbContext.OnModelCreating()
        public DateTimeOffset CreateDateTime { get; set; }

        [Required]
        public string Status { get; set; }

        /// <summary>
        /// This path must be accessible to MAP application and reduction server
        /// </summary>
        [Required]
        public string MasterContentFile { get; set; }

        /// <summary>
        /// null if reduction not requested.  Path must be accessible to MAP application and reduction server
        /// </summary>
        public string ResultReducedContentFile { get; set; }

        [Column(TypeName = "jsonb")]
        public string ResultHierarchy { get; set; }

        [Column(TypeName ="jsonb")]
        public string SelectionCriteria { get; set; }
    }
}


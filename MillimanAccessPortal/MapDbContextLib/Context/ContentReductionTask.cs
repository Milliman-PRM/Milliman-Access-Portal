using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace MapDbContextLib.Context
{
    public class ContentReductionTask
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [Column(TypeName = "timestamp with time zone")]
        // Default value is enforced in ApplicationDbContext.OnModelCreating()
        public DateTime CreateDateTime { get; set; }

        [Required]
        public string Status { get; set; }

    }
}


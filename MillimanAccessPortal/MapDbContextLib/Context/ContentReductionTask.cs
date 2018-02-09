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
        // default value defined in ApplicationDbContext.OnModelCreating()
        public DateTime CreateDateTime { get; set; }

        [Required]
        public string Status { get; set; }

    }
}


/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a MAP file drop resource
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class FileDrop
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Column(TypeName = "citext")]
        public string RootPath { get; set; }

        [ForeignKey("Client")]
        public Guid ClientId { get; set; }
        public Client Client { get; set; }

    }
}

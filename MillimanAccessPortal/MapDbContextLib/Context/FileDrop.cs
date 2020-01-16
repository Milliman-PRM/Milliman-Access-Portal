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

        public string Name { get; set; }

        public string RootPath { get; set; }

        [ForeignKey("Client")]
        public Guid ClientId { get; set; }
        public FileDrop Client { get; set; }

    }
}

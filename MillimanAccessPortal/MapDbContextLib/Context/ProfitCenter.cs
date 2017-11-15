/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a Client organization
 * DEVELOPER NOTES: 
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class ProfitCenter
    {
        public long Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string ProfitCenterCode { get; set; }

        public string MillimanOffice { get; set; }

        public string ContactName { get; set; }

        public string ContactTitle { get; set; }

        public string ContactEmail { get; set; }

        public string ContactPhone { get; set; }
    }
}

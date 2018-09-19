using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MapQueryAdminWeb.Pages
{
    public class QueryModel : PageModel
    {
        [Display(Name="Database Username")]
        [Required]
        public string pgsqlUsername { get; set; }
        [Display(Name ="Database Password")]
        [Required]
        public string pgsqlPassword { get; set; }
        [Display(Name ="Reference URL")]
        [Required]
        public string referenceUrl { get; set; }
        [Display(Name ="Approver Name")]
        [Required]
        public string approverName { get; set; }
        [Display(Name ="Query Text")]
        [Required]
        public string queryText { get; set; }

        public void OnGet()
        {

        }

    }
}
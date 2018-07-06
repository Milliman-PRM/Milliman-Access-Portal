/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: Model root content item details available to users
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemDetail
    {
        public long Id { get; set; }

        [HiddenInput]
        public long ClientId { get; set; }

        [Required]
        [Display(Name = "Content Name")]
        public string ContentName { get; set; }

        [Range(1, Int64.MaxValue, ErrorMessage = "You must select a content type")]
        [Display(Name = "Content Type")]
        public long ContentTypeId { get; set; }

        [Display(Name = "Does Reduce")]
        public bool DoesReduce { get; set; }

        public string Description { get; set; }

        public string Notes { get; set; }

        internal static RootContentItemDetail Build(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            RootContentItemDetail model = new RootContentItemDetail
            {
                Id = rootContentItem.Id,
                ClientId = rootContentItem.ClientId,
                ContentName = rootContentItem.ContentName,
                ContentTypeId = rootContentItem.ContentTypeId,
                DoesReduce = rootContentItem.DoesReduce,
                Description = rootContentItem.Description,
                Notes = rootContentItem.Notes,
            };

            return model;
        }
    }
}

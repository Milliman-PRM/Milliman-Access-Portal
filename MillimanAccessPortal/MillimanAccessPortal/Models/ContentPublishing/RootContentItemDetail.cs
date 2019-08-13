/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: Model root content item details available to users
 * DEVELOPER NOTES:
 */

using MapDbContextLib.Models;
using MillimanAccessPortal.Models.EntityModels.ContentItemModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.ContentPublishing
{
    public class RootContentItemDetail
    {
        [HiddenInput]
        public Guid ClientId { get; set; }

        public string ContentDisclaimer { get; set; }

        [Required]
        [Display(Name = "Content Name *")]
        public string ContentName { get; set; }

        [Display(Name = "Content Type *")]
        public Guid ContentTypeId { get; set; }

        public string ContentDescription { get; set; }

        [Display(Name = "Does Reduce")]
        public bool DoesReduce { get; set; }

        public Guid Id { get; set; }

        public bool IsSuspended { get; set; }

        public string ContentNotes { get; set; }

        public Dictionary<string, ContentRelatedFile> RelatedFiles { get; set; } = new Dictionary<string, ContentRelatedFile>();

        public Dictionary<Guid, RequestedAssociatedFile> AssociatedFiles { get; set; }

        public TypeSpecificContentItemProperties TypeSpecificDetailObject { get; set; }

    }
}

/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE:
 * DEVELOPER NOTES:
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using MillimanAccessPortal.Models.AccountViewModels;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminSelectionGroupListViewModel
    {
        public List<ContentAccessAdminSelectionGroupDetailViewModel> SelectionGroupList = new List<ContentAccessAdminSelectionGroupDetailViewModel>();
        public long RelevantRootContentItemId { get; set; } = -1;

        internal static ContentAccessAdminSelectionGroupListViewModel Build(ApplicationDbContext dbContext, RootContentItem rootContentItem)
        {
            var model = new ContentAccessAdminSelectionGroupListViewModel();

            var selectionGroups = dbContext.SelectionGroup
                .Where(sg => sg.RootContentItemId == rootContentItem.Id)
                .ToList();

            foreach (var selectionGroup in selectionGroups)
            {
                model.SelectionGroupList.Add(
                    ContentAccessAdminSelectionGroupDetailViewModel.Build(dbContext, selectionGroup)
                    );
            }

            return model;
        }
    }
}

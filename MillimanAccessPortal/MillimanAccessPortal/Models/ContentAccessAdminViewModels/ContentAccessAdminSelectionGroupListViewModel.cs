/*
 * CODE OWNERS: Joseph Sweeney
 * OBJECTIVE: A ViewModel for MAP
 * DEVELOPER NOTES:
 */

using System.Collections.Generic;
using System.Linq;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ContentAccessAdminViewModels
{
    public class ContentAccessAdminSelectionGroupListViewModel
    {
        public List<ContentAccessAdminSelectionGroupDetailViewModel> SelectionGroupList = new List<ContentAccessAdminSelectionGroupDetailViewModel>();
        public long RelevantRootContentItemId { get; set; } = -1;

        internal static ContentAccessAdminSelectionGroupListViewModel Build(ApplicationDbContext DbContext, RootContentItem RootContentItem)
        {
            ContentAccessAdminSelectionGroupListViewModel Model = new ContentAccessAdminSelectionGroupListViewModel();

            List<SelectionGroup> SelectionGroups = DbContext.SelectionGroup
                .Where(sg => sg.RootContentItemId == RootContentItem.Id)
                .ToList();

            foreach (var SelectionGroup in SelectionGroups)
            {
                Model.SelectionGroupList.Add(
                    ContentAccessAdminSelectionGroupDetailViewModel.Build(DbContext, SelectionGroup)
                    );
            }

            return Model;
        }
    }
}
